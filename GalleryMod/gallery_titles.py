"""Chinese captions for CG assets, keyed by their original asset family."""

import re


FAMILIES = """
trump 营地牌局
cutMage 法师特写
cutHero 勇者特写
cutPriest 祭司特写
cutWarrior 战士特写
trumpCardUra 营地牌局·牌背
trumpCard9 营地牌局·亮出的九点牌
trumpCardOnlyHand 营地牌局·手中的牌
trumpCard9Hand 营地牌局·手中的九点牌
partyScene 营地里的四位同伴
warriorTakusiage 战士整理衣袖
dosu 营地里的短刀
heroKokeMogu 勇者尝苔藓
meSasareDeathCutMe 被刺中的主角
heroKokeSwordInFire 篝火旁的勇者与剑
meHand 主角伸出的手
heroCasinoMe 游戏室里的勇者
villainessHeroTeaSetMe 恶役千金的茶会·勇者与主角
heroDropout 勇者与退队者
kokedama 苔藓团
heroSlimeBare 勇者的秘密
heroSlimeBareKui 勇者把苔藓送到嘴边
slimeHeroKokeChips 苔藓薯片
slimeHeroMaounoSugata 想象中的魔王
slimeHeroOpenMouth 勇者张开嘴
slimeHeroOpenMouthSlimeDetekuru 史莱姆从嘴里现身
slimeHeroOpenMouthSlimeDemakuri 史莱姆涌出
unknown_cave 陌生的洞穴
slimeHeroHonWatasareru 递过来的书
slimeHeroSlimeKiru 勇者挥剑斩向史莱姆
hero4BadEffect 勇者第四阶段坏结局·画面异变
hero4BadMe 勇者第四阶段坏结局·主角
hero4BadTatakuKageMe 勇者第四阶段坏结局·挥击的影子
hero4BadTatakuMe 勇者第四阶段坏结局·挥击
slimeHeroHonHutarideYomu 两人一起读书
slimeHeroKokeryouriHutariMe 两人一起做苔藓料理
slimeHeroMePriestKirikakaruMe 向祭司挥剑
slimeHeroSitaiMiorosuHutariMe 两人俯视倒下的人
hero5souVillainessDefeatedCutMetozi 落败的恶役千金·闭眼
hero5souVillainessDefeatedCutMeake 落败的恶役千金·睁眼
hero5souDropoutKaisou 勇者回想退队者
hero5souDropoutKaisou2WithOutcast 旧队伍的回忆·退队者与流放者
hero5souDropoutKaisou2WithOutcastAndVillainess 旧队伍的回忆·三人
hero5souDropoutDefeatedCutMetozi 落败的退队者·闭眼
hero5souDropoutDefeatedCutMeake 落败的退队者·睁眼
hero5souIronnaHitogaSinda 逝去的人们
hero5souCrossFailedHand 十字之约·手的特写
hero5souCrossFailedMe 十字之约·主角
hero5souEndSotonidetaTyokugoAkusyuMe 离开地牢·两人握手
hero5souEndSotonidetaTyokugoAkusyuTedasuMe 离开地牢·伸手相邀
hero5souEndSotonidetaTyokugoMe 离开地牢·勇者与主角
hero5souPriestHutarideKiruMe 两人挥剑迎战祭司
hero5souPriestHutarideKiruSwordOsameruMe 祭司一战之后·收剑
hero5souPriestHutarideKiruSwordOsameruSouryoKieruMe 祭司一战之后·身影消散
hero5souPriestHutarideKiruSwordOsameruSouryoKieruEffect 祭司一战之后·消散光效
heroTrialEnd 勇者线·试玩结尾
heroGasyan 勇者线·撞击
itemByDropout 退队者交付的物品
outcastDefeat_hand 流放者落败·手的特写
outcastDefeat_down 流放者落败·低下头
outcastDefeat_eye 流放者落败·眼神
outcastDefeat_lookUp 流放者落败·抬起头
outcastDefeat_sword 流放者落败·剑的特写
heroBackSword 背对主角的持剑勇者
outcastDefeat_heroSmile 流放者一战之后·勇者的笑
heroKokeSword 勇者的剑与苔藓
meHandHeroNigirare 勇者握住主角的手
heroToSlash 勇者挥剑
heroYes 勇者的答复
outcastPast 流放者的往事
outcastPast3_mage 流放者的往事·法师
outcastPast3_outcast 流放者的往事·流放者
outcastPast3_warrior 流放者的往事·战士
outcastPast3_party 流放者的往事·原来的小队
kokedango 苔藓团子
heroNiko 勇者微笑
MeHakike 主角忍住反胃
magePokerCard 法师牌局·扑克牌
magePoker 法师坐在牌桌前
magePokerCard5to 法师牌局·五张牌
mageCardUra 法师牌局·牌背
mageCardOmote 法师牌局·亮牌
meHaziku 主角拨牌
mage2sou01Me 途中休息时的交谈
mage2sou02 法师拿起人偶
mage2sou03Me 房间角落的长椅
mage2sou04Me 抵着额头的木桩
mage2sou05 法师跑在前面
magePokerKamisukiMe 法师牌局·拨开头发
villainessTeaPartyMageRouteMe 恶役千金的茶会·法师与主角
dropoutMageAoihikari 退队者事件·蓝光
dropoutMageKage 退队者事件·法师的影子
dropoutMageSitenHand 法师视角·伸手
dropoutMageSitenHat 法师视角·帽檐
dropoutMageEliteMe 与精英怪遭遇·法师线
dropoutMageMae 法师挡在前面
dropoutMageMaeHurimuki 法师回头
dropoutMageSitenOwariMe 法师视角·退队者事件之后
mage5souYubikiriTenasiKutiake 拉钩之约·开口
mage5souYubikiriTenasiMetozi 拉钩之约·闭眼
mage5souYubikiriTenasiOKuti 拉钩之约·惊讶
mage5souYubikiriTenasiMehosomeEmi 拉钩之约·眯眼微笑
mage5souYubikiriTenasiTere 拉钩之约·害羞
mage5souYubikiriTenasi 拉钩之约·法师的神情
mage5souYubikiriKamae 拉钩之约·伸出小指
mage5souYubikiriSuru 拉钩之约·两人拉钩
mage5souYubikiriTedake 拉钩之约·手部特写
mageMaryokunasiMiniGakuMae 非魔力线·画框前
meDeadZangai 倒在地上的主角
mage5souKorehaChanceDa 法师发现转机
mage5souMageNoTewoHiiteNigeruMehiraki 拉着法师逃走·回望
mage5souMageNoTewoHiiteNigeru 拉着法师逃走
mage5souKabeBakuhatsu 墙壁在身后炸开
mage5SouPriestToujouWithHero 祭司与勇者现身
mage5SouPriestToujou 祭司现身
mage5souYuujouMageKuiMitsume 友情尾声·法师的凝视
mage5souAibouNandakaraEgao 我们是搭档·笑容
mage5souAibouNandakara 我们是搭档
mage5souBadMageKubiKakaeMe 未赴约坏结局·抱住法师
mage5souBadSwordSasuMe 未赴约坏结局·剑刺下
mage5souKuroiHikariGaRyuuseiNoYouniAtowoOuMe 流星般追来的黑光
mage5SouSitaiTazusaeteWarauPriestMe 带着尸体微笑的祭司
mage5souSonomamaTaorekomeMe 力竭倒下
mage5souYuujou7ninSeizoroiMe 友情尾声·七人齐聚
mage5souYuujouTakibiMiageMe 友情尾声·篝火旁抬头
mage5souYuujouTakibiMe 友情尾声·篝火旁
mageNotMaryokuWinEndMe 非魔力线·特殊胜利
mageSmileGoblinBack 哥布林群中的法师
mageTaore 倒下的法师
outcastKuwareru_kaoAri 流放者被吞噬·面部显露
outcastKuwareru 流放者被吞噬
huseguMe 主角挡下攻击
mageCamp 法师线·营地休息
mageTrialEnd 法师线·试玩结尾
MeTaoreMioroshi 俯视倒下的主角
mage4souNotMaryokuMageYorikakariCutTere 法师倚过来·羞涩
mage4souNotMaryokuMageYorikakariCutEmi 法师倚过来·微笑
mage4souNotMaryokuMageYorikakariCut 法师倚过来·面部特写
mage5souWatashinokotogasukiNandarou 你是喜欢我吧
mage5souKyuuen 增援赶到
mageEplogueNotMaryokuPartyWithNewMember 尾声·加入新成员的小队
mage4souNotMaryokuKokuhakuKissMe 告白之后的吻
mage4souNotMaryokuKokuhakuMe 史莱姆事件后的告白
mage4souNotMaryokuMageYorikakariMe 法师倚在主角身旁
mage5souNotMaryokuLoveCutEgaoMe 恋爱尾声·法师的笑容
mage5souNotMaryokuLoveCutMe 恋爱尾声·法师特写
mage5souNotMaryokuSlimePriestMe 非魔力线·史莱姆与祭司
mage5souNotMaryokuSlimePriestSlashMe 非魔力线·迎战祭司
mageEplogueNotMaryokuLoveBedMe 恋爱尾声·两人的早晨
mage5souMagicUse 法师施展魔法
mage5souMagicAfter 魔法施展之后
mageEplogueHonKaki 魔力尾声·写书
mageEplogueHonToziMe 魔力尾声·合上书
mageEplogueHonTozi 魔力尾声·合上书的手
mageEplogueTegami 魔力尾声·来信
mageEplogueRyouri 魔力尾声·做饭
mageEplogueDoor 魔力尾声·门前
mageEplogueDoorHiraki 魔力尾声·门被推开
mageEplogueMageDoorHairuEgao 魔力尾声·来访者的笑
mageEplogueMageDoorHairu 魔力尾声·法师走进来
mageEplogueMadoNagameMe 魔力尾声·望向窗外
priest2Sou 祭司线高地篇
priestDropoutShinziteKureta 退队者事件·愿意相信
priestDropoutKyoukiSugaru 退队者事件·紧抓不放
priestDropoutAfterBattle 退队者一战后的祭司
priestDropoutAruasaMe 祭司的回忆·某一天早上
priestDropoutKaoMiteokouMe 临走前再看一眼
priestDropoutNigemasyouMe 一起逃走吧
priest4souKaisouPriestHouseNekorogariNamidaMetozi 祭司独自在家·含泪闭眼
priest4souKaisouPriestHouseNekorogariNamida 祭司独自在家·落泪
priest4souKaisouPriestHouseNekorogari 祭司独自在家·躺下
priest4souTenseiKaisouKaisi 祭司的转生回忆·开端
priest4souTenseiKaisouKodomoZidai 祭司的转生回忆·童年
priest4souTenseiKaisouOtonaZidai 祭司的转生回忆·成年
priest4souLowSanMiorosuPriest 低理智分支·俯视主角的祭司
priest4souLowSanWatashihaDaikiraiDesuyo 低理智分支·祭司的厌恶
priest4souLowSanGameOverKanokeBack 低理智分支·棺木的背面
priest4souLowSanGameOverKanokeHand 低理智分支·棺木边的手
priest4souLowSanGameOverKanoke 低理智分支·棺木
priest4souPriestSlimeKuware 祭司被史莱姆吞噬
priest4souMeKakusei 主角觉醒
priest4souKyuusyutsuTewotoru 从史莱姆中救人·拉住手
priest4souKyuusyutsuAtoCut 救出祭司之后
priest4souKyuusyutsuMe 从史莱姆中救出祭司
cutMeDoudou 主角挺身站定
priest4souLowSanDakisimeruMe 低理智分支·拥抱
priest4souLowSanKeritsukeruMe 低理智分支·踢开
priest4souLowSanKurutteyagaruMe 低理智分支·失控
priest4souLowSanNisemonoJanaiMe 低理智分支·你并非赝品
priest4souLowSanSuberakanaTeMe 低理智分支·光滑的手
priest4souLowSanSugaritukuMe 低理智分支·紧紧抓住
priest4souMeKaisouMadomiruOmoideMe 主角的回忆·望着窗外
priest4souMeMiorosiPriestMe 主角低头看着祭司
priestLowSanTadasiiHitoMe 低理智分支·所谓正确的人
priest5souDeadCutMehirakiAse 倒下的祭司·睁眼
priest5souDeadCutEmi 倒下的祭司·微笑
priest5souDeadCut 倒下的祭司·特写
priest5souDeadMe 祭司倒在主角面前
priest5souHighLoopMudaDesuyo 高循环·徒劳的尝试
priest5souHighLoopIkuraSippaisitemo 高循环·无论失败多少次
priest5souHighLoopKenniNegai 高循环·向剑许愿
priest5souHighLoopMiorosu 高循环·俯身凝望
priest5souHighLoopDakaratte 高循环·祭司的回应
priest5souHighLoopSugaruTe 高循环·紧抓不放的手
priest5souHighLoopEpilogueKotobaAfureru 高循环尾声·话语涌出
priestEpilogueCurtainBack 祭司尾声·帘后的身影
priestEpilogueCurtainTozi 祭司尾声·合拢的帘幕
priestEpilogueCurtain 祭司尾声·窗帘
priest5souHighLoopChoiceMugonMe 高循环抉择·沉默
priest5souHighLoopChoiceSayonaraMe 高循环抉择·道别
priest5souHighLoopChoiceSukiMe 高循环抉择·坦白心意
priest5souHighLoopEpilogueHebaritukuTiMe 高循环尾声·干涸的血迹
priest5souHighLoopEpilogueMiageruSoraMe 高循环尾声·仰望天空
priest5souHighLoopOwarinisimasyouMe 高循环·结束这一切吧
priest5souHighLoopUwagotoMe 高循环·喃喃自语
priest5souHighLoopTetsunagiUpMe 高循环·牵手特写
priest5souHighLoopTetsunagiMe 高循环·牵住彼此的手
priest5souTetsunagiUpMe 祭司牵手特写
priest5souTetsunagiMe 祭司牵起主角的手
priest5souToikakeMe 祭司在最后的发问
priestEpliogueBadMe 祭司尾声·坏结局
priestEpliogueDakisimeMe 祭司尾声·拥抱
priestKaisou 祭司的往事
priestWithKoke 祭司与苔藓
takeoniku 带来的肉
priestSolo 独自一人的祭司
killHeroAndMage 倒下的勇者与法师
priestHand 祭司的手
priestMemoto 祭司的双眼
priestTrialEnd 祭司线·试玩结尾
priestVsMe 主角面对祭司
priestWithMe 主角与祭司同行
villainessWarrior 战士与恶役千金的往事
warrior4souMiagetaraSitai 战士抬头看见尸体
warrior4souSwordKiru 战士挥剑
warrior4souSwordKittaato 剑落之后
warrior4souSwordKiruGlitch 挥剑时的画面故障
warriorKakuseiCutMetozi 战士觉醒·闭眼特写
warriorKakuseiCut 战士觉醒·近景
cutWarrior5Hand 魔王城里的战士·手部
cutWarrior5Hurikaeri 魔王城里的战士·回头
warrior5IdolEpilogueHakoniwa 偶像尾声·箱庭
warrior5IdolEpilogue 偶像结局的尾声
warriorGlitchBad 战士线·故障坏结局
warrior5IdolBeforeMageMe 偶像登台前·法师与主角
warrior5IdolBeforeMe 偶像登台前·主角
warrior5KatidesuMe 最后一场决斗·我赢了
warrior5souIllusionBAfterHydraAkarukuWaratte 幻境·九头蛇之战后的笑
warrior5souIllusionBAfterHydraDekirukaraYaru 幻境·因为能做到
warrior5souIllusionBAfterHydraKokyouniKaeru 幻境·回到故乡
warrior5souIllusionBAfterHydraMutekiJanaiMe 幻境·我们并非无敌
warrior5souIllusionBAfterHydraTokkunoMukasi 幻境·很久以前
warrior5souIllusionBHydraDefeatedTouzokuga 幻境·九头蛇倒下
warrior5souIllusionBOutcastRescueBakudanNageru 幻境·掷出炸弹
warrior5souIllusionBOutcastRescueBakuhatsu 幻境·爆炸救援
warrior5souIllusionBStartBoukenGainen 幻境·冒险的开端
warrior5souIllusionBStartKutinonakaniBakudan 幻境·嘴里的炸弹
warrior5souNightEndMe 第五层的夜谈
warriorStopBattleCutMe 主角拦下战斗
warriorStopBattleWarriorHand 战士准备停手
warriorVillainessKaisou 战士回忆恶役千金
warriorVsOutcast 战士对阵流放者
warrirorOutCastWin 战士击败流放者
warriorEyeCut 战士的眼神
cutVillainessMotoEgao 恶役千金·原来的笑容
cutVillainessMagao 恶役千金·收起笑容
warriorMioroshi 战士俯视对手
warriorTeage 战士举手
warriorTrialEnd 战士线·试玩结尾
outcastKutimotoAkeBig 流放者的嘴部·张大
outcastKutimotoAke 流放者的嘴部·微张
outcastKutimotoSakebi 流放者的嘴部·喊叫
outcastKutimotoTozi 流放者的嘴部·紧闭
outcastDefeat_blood 流放者落败·血迹
outcastSword 流放者的剑
outcastEye 流放者的眼神
outcastHurioroshi 流放者挥剑劈下
outcastStart 流放者初次现身
cutOutcast 流放者特写
darkKaigiMe 昏暗处的商议
villainessBoots 恶役千金的靴子
villainessNozokikomu 恶役千金俯身打量
villainessHurimuki 恶役千金回头
villainessClap 恶役千金拍手
villainessDrinkTeaMetozi 恶役千金闭眼品茶
villainessDrinkTea 恶役千金端起茶杯
gameroom 高地的游戏室
villainessStart 恶役千金现身
villainessSword 恶役千金的剑
villainessSlash 恶役千金挥剑
villainessHand 恶役千金伸手
villainessCorpse 倒下的恶役千金
MeDrinkTeaMetozi 主角闭眼品茶
MeDrinkTea 主角端起茶杯
villainerrHensinCut 异变中的恶役千金·特写
villainerrHensin 恶役千金发生异变
villainessAppear 恶役千金走近
villainessKissCut 恶役千金的吻
warriorHeadThrow 战士扔出首级
dropoutAppear 退队者现身
ghostShip 海上的幽灵船
ghost_ship_room 幽灵船的船舱
seaDragonTotsugeki 海龙突袭
dropoutBadDeadMe 退队者坏结局·倒下的主角
dropoutBadHakusyu 退队者坏结局·鼓掌
dropoutBadHoukoku 退队者坏结局·报告
dropoutBadToujou 退队者坏结局·登场
indoor_night 夜里的室内
slimeAppearMe 主角目睹史莱姆现身
demonCastle 魔王城
indoor_sunny 日光照进房间
warrior5_gyokuzaBack_Kiduki 王座上的战士·察觉来人
warrior5_gyokuzaBack 王座上的战士·背影
warrior5_gyokuzaSmile 王座上的战士·微笑
dropoutAllyCutLight 退队者前来助阵·光效
dropoutAllyCut 退队者前来助阵
outcastAllyCutLight 流放者前来助阵·光效
outcastAllyCut 流放者前来助阵
outcastRescueCut 流放者前来救援
villainessAllyCutLight 恶役千金前来助阵·光效
villainessAllyCut 恶役千金前来助阵
villainessRescueCut 恶役千金前来救援
corpses_ 地牢中倒下的队友
redSlimeComming 红色史莱姆逼近
priestOnDeadDark 祭司俯身看向主角·暗处
priestOnDeadHandDark 祭司向主角伸手·暗处
priestOnDeadHand 祭司向主角伸手
deadAfterMeChoice 选择外观后·倒地的主角
deadMeAfterChoice 选择外观后·倒地的主角
cutinMe 主角特写
kariEnd 过场插画
meChoice 主角外观选择
priest5souMaouClearMe 祭司线·击败魔王
endingTitleHeroMe 勇者结局·标题画面
endingTitleMageFriendshipMe 法师友情结局·标题画面
endingTitleMageLoveMe 法师恋爱结局·标题画面
endingTitleMageMaryokuMe 法师魔力结局·标题画面
endingTitlePriestSlowLifeDarkMe 慢生活低理智分支·标题画面
endingTitlePriestSlowLifeMe 祭司慢生活结局·标题画面
endingTitleWarriorIdolMe 战士偶像结局·标题画面
title-Me 主角版标题画面
title 默认标题画面
"""

CAPTIONS = dict(line.casefold().split(" ", 1) for line in FAMILIES.strip().splitlines())
PRIEST_STORY = (
    (4, "前往高地"),
    (6, "遇见恶役千金"),
    (18, "听祭司讲述往事"),
    (21, "旅途间歇"),
    (22, "恶役千金的试炼"),
    (23, "精英战后"),
    (30, "勇者与法师一战之后"),
)
WARRIOR_STORY = (
    (12, "往事上篇"),
    (21, "往事下篇"),
    (22, "高地的试炼"),
    (30, "往事续篇"),
    (37, "往事终篇"),
    (38, "迎战恶役千金"),
    (39, "提议会合"),
    (40, "交手之后"),
)


def caption(asset):
    """Return a scene caption and the original protagonist sprite variant (if designated)."""
    name = re.sub(r"\{\w+\}", "", asset).casefold()
    appearance = re.search(r"me([12])(?!\d)", name)
    if appearance:
        name = name[:appearance.start(1)] + name[appearance.end(1):]
    elif name.startswith("mechoice"):
        appearance = re.match(r"mechoice([12])$", name)
        if appearance:
            name = "mechoice"

    # In the two numbered sequences, retain shot numbers so variations can be found again.
    series = re.fullmatch(r"(priest2sou|villainesswarrior)0*(\d+)(?:me)?([_-]?[a-z0-9]+)?", name)
    if series:
        chapter, shot_text, alternate = series.groups()
        shot = int(shot_text)
        sections = PRIEST_STORY if chapter == "priest2sou" else WARRIOR_STORY
        section = next((title for last, title in sections if shot <= last), "尾声")
        detail = f"{CAPTIONS[chapter]}·{section} {shot:02}"
        if alternate:
            value = re.search(r"\d+$", alternate)
            detail += f"（镜头 {value[0]}）" if value else ("（抚过脸颊的手）" if alternate == "_kaonazorute" else "（近景）")
    else:
        detail = CAPTIONS.get(name)
        if detail is None:
            for base in ART_NAMES:
                if not name.startswith(base):
                    continue
                tail = name[len(base):]
                if re.fullmatch(r"(?:\d+(?:-\d+)?|cut\d*|(?:[0-9]+)?me(?:[0-9]+)?|[-_]\d+(?:me)?)", tail):
                    part = re.search(r"\d+(?:-\d+)?", tail)
                    tag = f"·镜头 {part[0].lstrip('0') or '0'}" if part else "·特写"
                    detail = CAPTIONS[base] + tag
                    break
            if detail is None:
                numbered = re.fullmatch(r"(.+?)(\d+)(me)", name)
                if numbered:
                    stem, part, protagonist = numbered.groups()
                    family = CAPTIONS.get(stem + protagonist)
                    if family:
                        detail = f"{family}·镜头 {int(part)}"
    return detail, int(appearance[1]) if appearance else 0


ART_NAMES = sorted(CAPTIONS, key=len, reverse=True)
