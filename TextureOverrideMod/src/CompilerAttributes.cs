namespace System.Runtime.CompilerServices;

// The Unity interop assembly exposes copies of these attributes without their constructors.
[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class NullableAttribute : Attribute
{
    public NullableAttribute(byte flag) { }
    public NullableAttribute(byte[] flags) { }
}

[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class NullableContextAttribute : Attribute
{
    public NullableContextAttribute(byte flag) { }
}
