using System.Reflection;

namespace Wyld
{
    public static class Consts
    {
        public static FieldInfo EffectParentField = typeof(Effect).GetField("Parent")!;
        public static FieldInfo EffectKField = typeof(Effect).GetField("K")!;

        public static FieldInfo EffectKStateField = typeof(Effect).GetField("KState")!;
        public static Keyword DefaultNamespaceName = Keyword.Intern("wyld.scratch");
        public static Keyword SystemNamespaceName = Keyword.Intern("wyld.system");
    }
}