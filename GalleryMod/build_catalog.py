"""Build the gallery index from the local Addressables catalog and art manifest."""

import argparse
from collections import defaultdict
import csv
from functools import lru_cache
import json
from pathlib import Path
import re
from struct import unpack_from
from gallery_titles import caption


def sprite_addresses(raw):
    """Read primary and internal IDs from a Unity Addressables binary v2 catalog."""
    def word(offset):
        return unpack_from("<I", raw, offset)[0]

    @lru_cache(maxsize=None)
    def string(offset, separator=""):
        if offset == 0xFFFFFFFF:
            return ""
        if offset & 0x40000000:
            parts = []
            while offset != 0xFFFFFFFF:
                part, offset = unpack_from("<II", raw, offset & 0x3FFFFFFF)
                parts.append(string(part))
            return separator.join(reversed(parts))
        start = offset & 0x3FFFFFFF
        size = word(start - 4)
        return raw[start:start + size].decode("utf-16le" if offset & 0x80000000 else "ascii")

    if word(4) != 2:
        raise ValueError("Expected the v2 Addressables catalog.")
    keys = word(8)
    locations = set()
    for row in range(keys, keys + word(keys - 4), 8):
        address_list = word(row + 4)
        locations.update(word(i) for i in range(address_list, address_list + word(address_list - 4), 4))

    addresses = defaultdict(set)
    for address in sorted(locations):
        type_id = word(address + 24)
        if type_id == 0xFFFFFFFF or string(word(type_id + 4), ".") != "UnityEngine.Sprite":
            continue
        key = string(word(address), "/")
        path = string(word(address + 4), "/")
        addresses[path].add(key)
    return addresses


def natural(text):
    return [int(part) if part.isdigit() else part.casefold() for part in re.split(r"(\d+)", text)]


def script_order(scripts):
    ordered = {}
    for name in ("ChoiceMe1", "ChoiceMe2", "Opening", "Opening2", "Opening3", "Dungeon",
                 *(f"Scenario_{route}{stage}" for route in ("Hero", "Mage", "Priest", "Warrior")
                   for stage in range(1, 6)), "Ending"):
        for _, line, command in scripts.get(name, ()):
            if command != "ModifyBackground":
                continue
            match = re.match(r'\s*@back\s+(?:"([^"]+)"|(\S+))', line)
            if match:
                asset = (match[1] or match[2]).rsplit("/", 1)[-1].casefold()
                ordered.setdefault(asset, len(ordered))
    return ordered


def build(manifest, addresses, ordered):
    routes = ("序章", "勇者线", "法师线", "祭司线", "战士线", "地牢事件", "共通素材", "结局与标题")
    entries = []
    seen = set()
    for row in manifest:
        category = row["分类"]
        if row["type"] != "Texture2D" or category not in ("CG", "结局与标题画面"):
            continue
        path = row["original_path"]
        if path in seen or not path:
            continue
        name = Path(path).stem
        if re.search(r"(?i)(glitchtex|glowtex|masktex|^toumei$)", name):
            continue
        keys = addresses.get(path)
        if not keys:
            raise ValueError(f"Missing catalog address for {path}")
        key = min(keys, key=lambda value: (
            not value.startswith("Naninovel/Backgrounds/MainBackground/"),
            value.rsplit("/", 1)[-1] != name,
            value,
        ))
        route = row["子类"] if category == "CG" else "结局与标题"
        chapter = row["细类"] if category == "CG" else row["子类"]
        title, appearance = caption(name)
        if title is None:
            raise ValueError(f"Missing Chinese caption for {name}")
        if appearance:
            title += "（长发）" if appearance == 1 else "（短发）"
        entries.append(dict(Route=route, Chapter=chapter, Title=title, AssetName=name, Address=key, Appearance=appearance))
        seen.add(path)

    entries.sort(key=lambda item: (
        routes.index(item["Route"]),
        natural(item["Chapter"]),
        ordered.get(item["AssetName"].casefold(), 999999),
        natural(item["AssetName"]),
    ))
    if not entries or len({entry["Address"] for entry in entries}) != len(entries):
        raise ValueError("Empty index or duplicate addresses.")
    if len({(entry["Route"], entry["Chapter"], entry["Title"]) for entry in entries}) != len(entries):
        raise ValueError("Duplicate CG captions within a gallery chapter.")
    return entries


def story_marks(scripts, gallery):
    routes = {"Hero": "勇者线", "Mage": "法师线", "Priest": "祭司线", "Warrior": "战士线"}
    scenes = {"Opening": "序章", "Opening2": "序章", "Opening3": "序章", "Ending": "结局",
              "Dungeon": "地牢事件", "Scenario": "共通剧情", "GameOver": "地牢事件"}
    images = {item["AssetName"].casefold(): item for item in gallery}
    for script, lines in scripts.items():
        match = re.fullmatch(r"Scenario_(Hero|Mage|Priest|Warrior)(\d+)", script)
        route = routes[match[1]] if match else scenes.get(script)
        if not route:
            continue
        for line, text, command in lines:
            if command != "ModifyBackground":
                continue
            appearance = re.match(r'\s*@back\s+(?:"([^"]+)"|(\S+))', text)
            if not appearance:
                continue
            name = appearance[1] or appearance[2]
            basename = name.rsplit("/", 1)[-1]
            if not (name.startswith("CG/") or basename.casefold() in images):
                continue
            if not basename or re.search(r"(?i)glitchtex|glowtex|masktex|^toumei$", basename):
                continue
            title, _ = caption(basename)
            if title is None:
                continue
            if re.search(r'(?i)tint:"?#(?:[0-9A-F]{6})00(?:"|\s|$)', text):
                continue
            target = re.search(r"\bid:([a-zA-Z0-9]+)", text)
            yield dict(Script=script, Line=line, Route=route, Title=title, Asset=basename,
                       Actor=target[1] if target else "MainBackground")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("catalog", type=Path)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("scripts", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--story-marks", type=Path)
    arguments = parser.parse_args()
    addresses = sprite_addresses(arguments.catalog.read_bytes())
    scripts = json.loads(arguments.scripts.read_text(encoding="utf-8-sig"))
    ordered = script_order(scripts)
    with arguments.manifest.open(encoding="utf-8-sig", newline="") as source:
        entries = build(csv.DictReader(source), addresses, ordered)
    arguments.output.write_text(json.dumps(entries, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    if arguments.story_marks:
        marks = list(story_marks(scripts, entries))
        arguments.story_marks.write_text(json.dumps(marks, ensure_ascii=False, separators=(",", ":")) + "\n", encoding="utf-8")
        print(f"{len(marks)} story CG marks indexed.")
    print(f"{len(entries)} CG entries indexed.")


if __name__ == "__main__":
    main()
