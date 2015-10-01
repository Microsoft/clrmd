﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    [TestClass]
    public class TypeTests
    {
        [TestMethod]
        public void ComponentType()
        {
            // Simply test that we can enumerate the heap.

            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrHeap heap = runtime.GetHeap();

                foreach (ulong obj in heap.EnumerateObjects())
                {
                    var type = heap.GetObjectType(obj);
                    Assert.IsNotNull(type);

                    if (type.IsArray || type.IsPointer)
                        Assert.IsNotNull(type.ComponentType);
                    else
                        Assert.IsNull(type.ComponentType);
                }
            }
        }


        [TestMethod]
        public void TypeEqualityTest()
        {
            // This test ensures that only one ClrType is created when we have a type loaded into two different AppDomains with two different
            // method tables.

            const string TypeName = "Foo";
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrHeap heap = runtime.GetHeap();

                ClrType[] types = (from obj in heap.EnumerateObjects()
                                   let t = heap.GetObjectType(obj)
                                   where t.Name == TypeName
                                   select t).ToArray();

                Assert.AreEqual(2, types.Length);
                Assert.AreEqual(types[0], types[1]);

                ClrModule module = runtime.EnumerateModules().Where(m => Path.GetFileName(m.FileName).Equals("sharedlibrary.dll", StringComparison.OrdinalIgnoreCase)).Single();
                ClrType typeFromModule = module.GetTypeByName(TypeName);

                Assert.AreEqual(TypeName, typeFromModule.Name);
                Assert.AreEqual(types[0], typeFromModule);
            }
        }

        [TestMethod]
        public void VariableRootTest()
        {
            // Test to make sure that a specific static and local variable exist.

            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrHeap heap = runtime.GetHeap();

                var fooRoots = from root in heap.EnumerateRoots()
                               where root.Type.Name == "Foo"
                               select root;

                ClrRoot staticRoot = fooRoots.Where(r => r.Kind == GCRootKind.StaticVar).Single();
                Assert.IsTrue(staticRoot.Name.Contains("s_foo"));

                ClrRoot localVarRoot = fooRoots.Where(r => r.Kind == GCRootKind.LocalVar).Single();

                ClrThread thread = runtime.GetMainThread();
                ClrStackFrame main = thread.GetFrame("Main");
                ClrStackFrame inner = thread.GetFrame("Inner");

                ulong low = thread.StackBase;
                ulong high = thread.StackLimit;

                // Account for different platform stack direction.
                if (low > high)
                {
                    ulong tmp = low;
                    low = high;
                    high = tmp;
                }


                Assert.IsTrue(low <= localVarRoot.Address && localVarRoot.Address <= high);
            }
        }

        [TestMethod]
        public void TypeHandleHeapEnumeration()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrHeap heap = runtime.GetHeap();

                foreach (ClrType type in heap.EnumerateObjects().Select(obj => heap.GetObjectType(obj)).Unique())
                {
                    Assert.AreNotEqual(0ul, type.TypeHandle);

                    ClrType typeFromHeap;
                    
                    if (type.IsArray)
                    {
                        ClrType componentType = type.ComponentType;
                        Assert.IsNotNull(componentType);
                        
                        typeFromHeap = heap.GetTypeByTypeHandle(type.TypeHandle, componentType.TypeHandle);
                    }
                    else
                    {
                        typeFromHeap = heap.GetTypeByTypeHandle(type.TypeHandle);
                    }

                    Assert.AreEqual(type.TypeHandle, typeFromHeap.TypeHandle);
                    Assert.AreSame(type, typeFromHeap);
                }
            }
        }

        [TestMethod]
        public void GetObjectTypeHandleTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrHeap heap = runtime.GetHeap();

                int i = 0;
                foreach (ulong obj in heap.EnumerateObjects())
                {
                    i++;
                    ClrType type = heap.GetObjectType(obj);

                    if (type.IsArray)
                    {
                        ulong mt, cmt;
                        bool result = heap.TryGetTypeHandle(obj, out mt, out cmt);

                        Assert.IsTrue(result);
                        Assert.AreNotEqual(0ul, mt);
                        Assert.AreEqual(type.TypeHandle, mt);
                        
                        Assert.AreSame(type, heap.GetTypeByTypeHandle(mt, cmt));
                    }
                    else
                    {
                        ulong mt = heap.GetTypeHandle(obj);
                        
                        Assert.AreNotEqual(0ul, mt);
                        Assert.IsTrue(type.EnumerateTypeHandles().Contains(mt));

                        Assert.AreSame(type, heap.GetTypeByTypeHandle(mt));
                        Assert.AreSame(type, heap.GetTypeByTypeHandle(mt, 0));

                        ulong mt2, cmt;
                        bool res = heap.TryGetTypeHandle(obj, out mt2, out cmt);

                        Assert.IsTrue(res);
                        Assert.AreEqual(mt, mt2);
                        Assert.AreEqual(0ul, cmt);
                    }
                }

            }
        }

        [TestMethod]
        public void EnumerateTypeHandleTest()
        {
            using (DataTarget dt = TestTargets.AppDomains.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrHeap heap = runtime.GetHeap();

                ulong[] fooObjects = (from obj in heap.EnumerateObjects()
                                      let t = heap.GetObjectType(obj)
                                      where t.Name == "Foo"
                                      select obj).ToArray();

                // There are exactly two Foo objects in the process, one in each app domain.
                // They will have different method tables.
                Assert.AreEqual(2, fooObjects.Length);


                ClrType fooType = heap.GetObjectType(fooObjects[0]);
                Assert.AreSame(fooType, heap.GetObjectType(fooObjects[1]));
                

                ClrRoot appDomainsFoo = (from root in heap.EnumerateRoots(true)
                                         where root.Kind == GCRootKind.StaticVar && root.Type == fooType
                                         select root).Single();

                ulong nestedExceptionFoo = fooObjects.Where(obj => obj != appDomainsFoo.Object).Single();
                ClrType nestedExceptionFooType = heap.GetObjectType(nestedExceptionFoo);

                Assert.AreSame(nestedExceptionFooType, appDomainsFoo.Type);

                ulong nestedExceptionFooMethodTable = dt.DataReader.ReadPointerUnsafe(nestedExceptionFoo);
                ulong appDomainsFooMethodTable = dt.DataReader.ReadPointerUnsafe(appDomainsFoo.Object);

                // These are in different domains and should have different type handles:
                Assert.AreNotEqual(nestedExceptionFooMethodTable, appDomainsFooMethodTable);

                // The TypeHandle returned by ClrType should always be the method table that lives in the "first"
                // AppDomain (in order of ClrAppDomain.Id).
                Assert.AreEqual(appDomainsFooMethodTable, fooType.TypeHandle);

                // Ensure that we enumerate two type handles and that they match the method tables we have above.
                ulong[] typeHandleEnumeration = fooType.EnumerateTypeHandles().ToArray();
                Assert.AreEqual(2, typeHandleEnumeration.Length);

                // These also need to be enumerated in ClrAppDomain.Id order
                Assert.AreEqual(appDomainsFooMethodTable, typeHandleEnumeration[0]);
                Assert.AreEqual(nestedExceptionFooMethodTable, typeHandleEnumeration[1]);
            }
        }

        [TestMethod]
        public void ArrayReferenceEnumeration()
        {
            using (DataTarget dt = TestTargets.Types.LoadFullDump())
            {
                ClrRuntime runtime = dt.ClrVersions.Single().CreateRuntime();
                ClrHeap heap = runtime.GetHeap();

                ClrAppDomain domain = runtime.AppDomains.Single();

                ClrModule typesModule = runtime.GetModule("types.exe");
                ClrType type = heap.GetTypeByName("Types");


                ulong s_array = (ulong)type.GetStaticFieldByName("s_array").GetValue(domain);
                ulong s_one = (ulong)type.GetStaticFieldByName("s_one").GetValue(domain);
                ulong s_two = (ulong)type.GetStaticFieldByName("s_two").GetValue(domain);
                ulong s_three = (ulong)type.GetStaticFieldByName("s_three").GetValue(domain);

                ClrType arrayType = heap.GetObjectType(s_array);

                List<ulong> objs = new List<ulong>();
                arrayType.EnumerateRefsOfObject(s_array, (obj, offs) => objs.Add(obj));


                Assert.AreEqual(3, objs.Count);
                Assert.IsTrue(objs.Contains(s_one));
                Assert.IsTrue(objs.Contains(s_two));
                Assert.IsTrue(objs.Contains(s_three));
            }
        }
    }
}
