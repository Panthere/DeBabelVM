using BabelVMRestore.Logger;
using BabelVMRestore.Structs;
using BabelVMRestore.Utilities;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BabelVMRestore.Core
{
    public class MethodRestorer
    {
        #region Fields
        private ModuleDefMD _module;
        private MethodDef _invokerMtd;

        #endregion

        #region Constructors
        public MethodRestorer()
            : this(null)
        {
        }

        public MethodRestorer(ModuleDefMD module)
        {
            _module = module;
        }

        #endregion

        #region Memebers
        #region Public Members
        public int Restore()
        {
            int totalChanges = 0;
            if (!FindInvokeMethod())
            {
                ConsoleLogger.Error("[!] Could not find Invoker Method! Cannot Continue!");
                return totalChanges;
            }

            List<EncryptedInfo> callerInfos = FindVMCallers();

            if (callerInfos.Count == 0)
            {
                ConsoleLogger.Error("[!] Could not find any VM Callers! Cannot Continue!");
                return totalChanges;
            }

            totalChanges += InvokeVMCallers(callerInfos);


            ConsoleLogger.Success("[!] Restored {0} methods from BabelVM", totalChanges);
            return totalChanges;
        }
        public void Write(string outputFile)
        {
            var opts = new ModuleWriterOptions(_module);
            opts.MetaDataOptions.Flags = MetaDataFlags.PreserveAll;
            opts.Logger = DummyLogger.NoThrowInstance;

            _module.Write(outputFile, opts);

            if (File.Exists(outputFile))
            {
                ConsoleLogger.Success("[!] Module saved");
            }
            else
            {
                ConsoleLogger.Error("[!] Module could not be saved!");
            }
        }
        #endregion
        #region Private Members
        private bool FindInvokeMethod()
        {
            foreach (TypeDef type in this._module.Types)
            {
                if (type.BaseType == null)
                    continue;
                if (!type.HasInterfaces)
                    continue;

                if (!type.Interfaces[0].Interface.FullName.Contains("IDisposable"))
                    continue;
                foreach (MethodDef md in type.Methods)
                {
                    if (!md.HasBody)
                        continue;
                    if (!md.IsPrivate)
                        continue;
                    if (md.IsStatic)
                        continue;
                    if (md.Parameters.Count < 2)
                        continue;

                    if (md.Parameters[1].Type.FullName != "System.Int32" && md.Parameters[1].Type.FullName != "System.String")
                        continue;

                    if (md.Body.ExceptionHandlers.Count != 1)
                        continue;

                    bool skipMethod = false;
                    for (int i = 0; i < md.Body.Instructions.Count; i++)
                    {
                        Instruction inst = md.Body.Instructions[i];

                        if (inst.OpCode == dnlib.DotNet.Emit.OpCodes.Ldstr)
                        {
                            if (((string)inst.Operand) != "Error dynamic method {0}: {1}")
                            {
                                skipMethod = true;
                                break;
                            }
                        }
                    }
                    if (skipMethod)
                        continue;

                    _invokerMtd = md;
                }
            }
            return _invokerMtd != null;
        }

        private List<EncryptedInfo> FindVMCallers()
        {
            List<EncryptedInfo> InvokeCallerInfo = new List<EncryptedInfo>();

            foreach (TypeDef type in this._module.Types)
            {
                foreach (MethodDef md in type.Methods)
                {
                    if (!md.HasBody)
                        continue;

                    EncryptedInfo info = new EncryptedInfo();
                    bool found = false;
                    for (int i = 0; i < md.Body.Instructions.Count; i++)
                    {
                        Instruction inst = md.Body.Instructions[i];

                        if (inst.Operand is MethodDef)
                        {
                            MethodDef mDef = inst.Operand as MethodDef;

                            if (mDef.Parameters.Count == 3)
                            {
                                if (mDef.Parameters[0].Type.FullName != "System.Int32")
                                    continue;
                                if (mDef.Parameters[1].Type.FullName != "System.Object")
                                    continue;
                                if (mDef.Parameters[2].Type.FullName != "System.Object[]")
                                    continue;
                            }
                            else if (mDef.Parameters.Count == 2)
                            {
                                if (mDef.Parameters[0].Type.FullName != "System.String")
                                    continue;
                                if (mDef.Parameters[1].Type.FullName != "System.Object[]")
                                    continue;
                            }
                            else
                            {
                                continue;
                            }

                            if (!mDef.IsStatic)
                                continue;
                            if (!mDef.IsPublic)
                                continue;
                            if (mDef.ReturnType.FullName != "System.Object")
                                continue;

                            info.Method = md;
                            found = true;
                        }
                    }
                    if (found)
                    {
                        ConsoleLogger.Verbose("[!] Encrypted Method Found - {0} (RVA: {1}, MDToken: 0x{2:X})", md.FullName, md.RVA, md.MDToken.ToInt32());
                        Instruction inst = md.Body.Instructions[0];
                        if (inst.OpCode == dnlib.DotNet.Emit.OpCodes.Ldc_I4)
                        {
                            if (info.Key != 0)
                                continue;
                            info.Key = inst.GetLdcI4Value();
                            info.VMType = EncType.Int;
                            InvokeCallerInfo.Add(info);
                        }
                        else if (inst.OpCode == dnlib.DotNet.Emit.OpCodes.Ldstr)
                        {
                            if (info.KeyString != null)
                            {
                                continue;
                            }
                            info.VMType = EncType.String;
                            info.KeyString = inst.Operand as string;
                            InvokeCallerInfo.Add(info);
                        }
                    }
                }
            }
            ConsoleLogger.Verbose("[!] Found {0} VM Callers", InvokeCallerInfo.Count);
            return InvokeCallerInfo;
        }

        private int InvokeVMCallers(List<EncryptedInfo> InvokeCallerInfo)
        {
            int changes = 0;
            bool gotFieldName = false;
            string fieldName = "";

            Assembly assembly = Assembly.LoadFile(Settings.FileName);
            MethodBase mb = assembly.ManifestModule.ResolveMethod(_invokerMtd.MDToken.ToInt32());
            ConstructorInfo c = mb.DeclaringType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);

            object a = c.Invoke(new object[] { });

            foreach (EncryptedInfo info in InvokeCallerInfo)
            {
                try
                {

                    object dr = mb.Invoke(a, new object[] { (info.VMType == EncType.Int ? (object)info.Key : (object)info.KeyString) });
                    Type drType = dr.GetType();

                    if (!gotFieldName)
                    {
                        foreach (FieldInfo fi in drType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                        {
                            if (fi.FieldType.FullName == "System.Reflection.Emit.DynamicMethod")
                            {
                                fieldName = fi.Name;
                                
                                ConsoleLogger.Info("[!] Found Dynamic Method Field: MDToken: 0x{0:X}", fi.MetadataToken);
                            }
                        }
                        if (string.IsNullOrEmpty(fieldName))
                        {
                            ConsoleLogger.Error("[!] Could not find Dynamic Method Field Name! Trying with \uE006 !", info.Method.RVA, info.Method.MDToken.ToInt32());
                            fieldName = "\uE006";
                        }
                        gotFieldName = true;
                    }

                    info.ResolvedDynamicMethod = ReflectionHelper.GetInstanceField(drType, dr, fieldName) as System.Reflection.Emit.DynamicMethod;

                    SuperDynamicReader mbr = new SuperDynamicReader(_module, info.ResolvedDynamicMethod);
                    mbr.Read();

                    info.ResolvedMethod = mbr.GetMethod();
                    info.Method.Body = info.ResolvedMethod.Body;

                    changes++;
                    ConsoleLogger.Verbose("[!] Encrypted Method Restored - {0} (RVA: {1}, MDToken: 0x{2:X})", info.Method.FullName, info.Method.RVA, info.Method.MDToken.ToInt32());

                }
                catch (Exception ex)
                {
                    ConsoleLogger.Info("[!] Failed Restoration 0x{1:X} : {0}", ex, info.Method.MDToken.ToInt32());
                }
            }
            return changes;
        }
        #endregion
        #endregion
    }
}
