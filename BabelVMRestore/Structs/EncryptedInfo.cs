
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Reflection.Emit;

namespace BabelVMRestore.Structs
{
    public class EncryptedInfo
    {
        public MethodDef Method;
        public int Key;
        public DynamicMethod ResolvedDynamicMethod;
        public MethodDef ResolvedMethod;
        public string KeyString;
        public EncType VMType;
    }
    public enum EncType
    {
        String,
        Int
    }
    public class CFlowInt
    {
        public MethodDef InvokerMethod;
        public MethodDef OwnerMethod;
        public Instruction CallInst;
        public Instruction KeyInst;
        public int Key;
    }
}
