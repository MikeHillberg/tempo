using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Tempo
{
    public class AsyncCounter
    {
        public int Value { get; set; } = 0;
    }

    public class TypeReferenceHelper
    {

        static public IList<TypeViewModel> FindReturnedByTypesClosure(TypeViewModel typeVM)
        {
            //var asyncCounter = new AsyncCounter();
            //foreach (var type in typeVM.TypeSet.Types)
            //{
            //    var allOutMembers = AllMembersWhereForType(
            //                            type,               
            //                            asyncCounter,
            //                            checkOutOnly: true,
            //                            (t) => true);

            //    var list = new List<TypeViewModel>();
            //    foreach(var member in allOutMembers)
            //    {
            //        if(!list.Contains(member.Item2))
            //        {
            //            list.Add(member.Item2);
            //        }
            //    }
            //}

            var typesToFollow = new List<TypeViewModel>()
            {
                typeVM
            };
            var referencingTypes = new List<TypeViewModel>();
            var counter = new AsyncCounter();

            while (typesToFollow.Count != 0)
            {
                Debug.WriteLine($"Types to follow: {typesToFollow.Count}");

                var copy = typesToFollow.ToArray();
                typesToFollow.Clear();
                foreach (var type in copy)
                {
                    var directReferencing = type.ReturnedByAsync;
                    foreach (var member in directReferencing)
                    {
                        var type2 = member.DeclaringType;
                        if (!referencingTypes.Contains(type2))
                        {
                            typesToFollow.Add(type2);
                            referencingTypes.Add(type2);
                        }
                    }
                }
            }

            return referencingTypes;
        }

        static public IEnumerable<TypeViewModel> FindReferencesToOtherNamespaces(AsyncCounter counter, string fromNamespace)
        {
            var matchingTypes = new List<TypeViewModel>();
            var members = AllMembersWhere(counter, /*checkOutOnly*/false,
                (type, _) =>
                {
                    var matches = !type.Namespace.StartsWith(fromNamespace); // Return types from other namespaces
                    if (matches && !matchingTypes.Contains(type))
                    {
                        matchingTypes.Add(type);
                    }
                    return matches;
                },
                (TypeViewModel type) => type.Namespace.StartsWith(fromNamespace)   // Only search types in the namespace
                );

            // bugbug: matchingTypes is a side-effect of enumerating
            members.ToList();
            return matchingTypes;
        }

        static public IEnumerable<MemberOrTypeViewModelBase> FindReturningMembers(TypeViewModel soughtType, AsyncCounter counter)
        {
            return AllMembersWhere(
                counter,
                /*checkOutOnly*/true,
                (type, _) => type == soughtType);
        }


        static public IEnumerable<MemberOrTypeViewModelBase> AllMembersWhere(
            AsyncCounter counter,
            bool checkOutOnly, // Only check [out] parameters
            Func<TypeViewModel, MemberViewModelBase, bool> typeCheck,
            Func<TypeViewModel, bool> typeFilter = null)
        {
            var initialCount = counter.Value;

            if (counter.Value != -1 && initialCount != counter.Value) yield break;

            foreach (var type in Manager.CurrentTypeSet.Types)
            {
                if (counter.Value != -1 && initialCount != counter.Value)
                {
                    yield break;
                }

                var members = AllMembersWhereForType(type, counter, checkOutOnly, typeCheck, typeFilter);
                if(members == null)
                {
                    continue;
                }

                foreach(var member in members)
                {
                    yield return member.Item1;
                }

            }
        }

        public static IEnumerable<(MemberOrTypeViewModelBase,TypeViewModel)> AllMembersWhereForType(
            TypeViewModel type,
            AsyncCounter counter,
            bool checkOutOnly, // Only check [out] parameters
            Func<TypeViewModel, MemberViewModelBase, bool> typeCheck,
            Func<TypeViewModel, bool> typeFilter = null)

        {
            if (typeFilter != null && !typeFilter(type))
            {
                yield break;
            }

            if (!type.IsPublic && !Manager.Settings.InternalInterfaces)
            {
                yield break;
            }

            //if (counter.Value != -1 && initialCount != counter.Value)
            //    yield break;

            foreach (var prop in type.Properties)
            {
                if (WalkTypesInAncestorsOrGenericArguments(prop.PropertyType, prop, typeCheck))
                {
                    yield return  (prop, prop.PropertyType);
                }
            }

            foreach (var ev in type.Events)
            {
                if (WalkTypesInAncestorsOrGenericArguments(ev.EventHandlerType, ev, typeCheck))
                {
                    yield return (ev, ev.EventHandlerType);
                }

                // If the event handler type is TypedEventHandler<TSender,TArgs>, and we're looking
                // for TArgs, then we just found it. But if the type is a custom delegate we didn't.
                // Ex if we're looking for SelectionChangedEventArgs and the event handler type is SelectionChangedHandler,
                // then we didn't just find it but we should. So check the parameters on the Invoke() method
                else if (ev.EventHandlerType.DelegateInvoker != null)
                {
                    var parameters = ev.EventHandlerType.DelegateInvoker.Parameters;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            if (WalkTypesInAncestorsOrGenericArguments(param.ParameterType, ev, typeCheck))
                            {
                                yield return (ev, param.ParameterType);
                                break;
                            }
                        }
                    }
                }
            }


            // For most types, look for [out] parameters. For Delegate types,
            // look at the Invoker method, which is essentially an "out"
            IList<MethodViewModel> methods;
            if(type.DelegateInvoker == null)
            {
                methods = type.Methods;
            }
            else
            {
                methods = new MethodViewModel[] { type.DelegateInvoker }.ToList();
            }

            foreach (var method in methods)
            {
                if (WalkTypesInAncestorsOrGenericArguments(method.ReturnType, method, typeCheck))
                {
                    yield return (method, method.ReturnType);
                    continue;
                }

                foreach (var param in method.Parameters)
                {
                    if ((param.IsOut || !checkOutOnly || type.DelegateInvoker != null)
                        && (WalkTypesInAncestorsOrGenericArguments(param.ParameterType, method, typeCheck)))
                    {
                        yield return (method, param.ParameterType);
                    }
                }
            }

            if (!checkOutOnly)
            {
                foreach (var constructor in type.Constructors)
                {
                    if (WalkTypesInAncestorsOrGenericArguments(constructor.ReturnType, constructor, typeCheck))
                    {
                        yield return (constructor, constructor.ReturnType);
                        continue;
                    }

                    foreach (var param in constructor.Parameters)
                    {
                        if (WalkTypesInAncestorsOrGenericArguments(param.ParameterType, constructor, typeCheck))
                        {
                            yield return (constructor, param.ParameterType);
                            break;
                        }
                    }
                }

                if (WalkTypesInAncestorsOrGenericArguments(type, null, typeCheck))
                {
                    yield return (type, type);
                }
            }

        }

        static bool WalkTypesInAncestorsOrGenericArguments(
            TypeViewModel candidateType, 
            MemberViewModelBase member,
            Func<TypeViewModel, MemberViewModelBase, bool>  check)
        {
            // bugbug: don't do early returns here when we find the first matching type, because one usage of this method
            // is to find all matching types.
            var result = false;

            if (candidateType.FullName == "System.Void")
                return false;
            else if (candidateType.FullName == "Windows.Foundation.IAsyncAction")
                return false;

            var ancestor = candidateType;
            while (ancestor != null
                    && ancestor.FullName != "System.Object"
                    && ancestor.FullName != "System.ValueType")
            {
                if (!ancestor.IsGenericType)
                {
                    if (check(ancestor, member))
                    {
                        result = true;
                    }
                }
                else
                {
                    // Ignore IAsync stuff, but still check for the <T> type
                    var genericTypeDefinition = ancestor.GetGenericTypeDefinition();
                    if (!genericTypeDefinition.FullName.StartsWith("Windows.Foundation.IAsync")
                        && check(genericTypeDefinition, member))
                    {
                        result = true;
                    }

                    // If searching for Foo, don't return Foo.BarEvent, even though the event args has a Foo-typed sender,
                    // because duh.
                    bool skipArg = false;
                    if (candidateType.PrettyFullName.StartsWith("Windows.Foundation.TypedEventHandler<"))
                    {
                        skipArg = true;
                    }

                    foreach (var arg in ancestor.GetGenericArguments())
                    {
                        if (skipArg)
                        {
                            skipArg = false;
                            continue;
                        }
                        if (WalkTypesInAncestorsOrGenericArguments(arg, member, check))
                            result = true;
                    }
                }

                ancestor = ancestor.BaseType;
            }

            var ifaces = candidateType.GetAllInterfaces();
            foreach (var iface in ifaces)
            {
                //if (candidateType == iface)
                if (iface.IsPublic && check(iface, member))
                {
                    result = true;
                }
            }

            return result;
        }


        //static bool IsTypeInTypeArguments(TypeViewModel soughtType, TypeViewModel candidateType)
        //{
        //    if (!candidateType.IsGenericType)
        //        return false;

        //    // bugbug: What is the difference between this method and FindTypeInAncestorsOrGenericArguments?
        //    // If searching for Foo, don't return Foo.BarEvent, even though the event args has a Foo-typed sender,
        //    // because it's too noisy.
        //    bool skipArg = false;
        //    if (candidateType.PrettyFullName.StartsWith("Windows.Foundation.TypedEventHandler<"))
        //    {
        //        skipArg = true;
        //    }

        //    foreach (var typeArg in candidateType.GetGenericArguments())
        //    {
        //        if(skipArg)
        //        {
        //            skipArg = false;
        //            continue;
        //        }

        //        if (typeArg == soughtType)
        //            return true;

        //        if (IsTypeInTypeArguments(soughtType, typeArg))
        //            return true;
        //    }

        //    return false;
        //}

        static public IEnumerable<TypeViewModel> FindReferencingTypes(
            TypeViewModel findType,
            int referenceIndex)
        {
            if (referenceIndex != -1 && referenceIndex != Manager._referenceIndex) yield break;

            foreach (var checkType in Manager.CurrentTypeSet.Types)
            {
                if (!checkType.IsPublic && !Manager.Settings.InternalInterfaces)
                    continue;


                if (referenceIndex != -1 && referenceIndex != Manager._referenceIndex)
                    yield break;

                if (checkType == findType)
                    continue;

                foreach (var referencedTypeAndSource in GetDirectReferencedTypes(checkType, false /* shouldFlatten */, false))
                {
                    if (IsTypeInType(findType, referencedTypeAndSource.TypeVM))
                    {
                        if (findType.IsXamlControl && !findType.CheckedAutomationPeer)
                        {
                            if (checkType.IsAutomationPeer)
                            {
                                foreach (var constructor in checkType.Constructors)
                                {
                                    if (constructor.Parameters.Count == 1
                                        &&
                                        constructor.Parameters[0].ParameterType.FullName == findType.FullName)
                                    {
                                        Action update = () =>
                                        {
                                            findType.CheckedAutomationPeer = true;
                                            findType.AutomationPeer = checkType;
                                        };

                                        if (Manager.PostToUIThread != null)
                                        {
                                            Manager.PostToUIThread(update);
                                        }
                                        else
                                        {
                                            update();
                                        }
                                    }
                                }
                            }
                        }

                        yield return checkType;
                        break;
                    }
                }

            }

            findType.CheckedAutomationPeer = true;
        }


        internal class TypeMap : Dictionary<TypeViewModel, string>
        {

        }

        internal class TypeList : Collection<TypeViewModel>
        {
        }

        internal class TypeAndSourceList : Collection<TypeAndSource>
        {
        }


        public class TypeAndSource
        {
            public TypeAndSource(TypeViewModel type, string source)
            {
                TypeVM = type;
                Source = source;
            }
            public TypeViewModel TypeVM { get; set; }
            public string Source { get; set; }
        }

        static public IEnumerable<TypeAndSource> GetDirectReferencedTypes(TypeViewModel findType, bool shouldFlatten, bool includeDerived)
        {
            var types = new TypeMap();

            foreach (var typeAndSource in GetDirectReferencedTypesHelper1(findType, shouldFlatten, includeDerived))
            {
                var typeVM = typeAndSource.TypeVM;

                if (typeVM.IsGenericParameter)
                    continue;

                if (typeVM.FullName == null)
                    continue;

                if (typeVM.FullName == "System.Void")
                    continue;

                if (typeVM.Name.EndsWith("&"))
                    continue;

                {
                    if (!types.ContainsKey(typeAndSource.TypeVM))
                        types.Add(typeAndSource.TypeVM, typeAndSource.Source);
                }
            }

            return from t in types select new TypeAndSource(t.Key, t.Value);
        }


        static internal bool IsTerminator(TypeViewModel typeVM)
        {
            if (typeVM == null || typeVM.ShouldIgnore)
                return true;

            if (typeVM.Namespace == "System")
                return true;

            return false;
        }


        static internal IEnumerable<TypeAndSource> GetDirectReferencedTypesHelper1(TypeViewModel findType, bool shouldFlatten, bool includeDerived)
        {
            foreach (var typeAndSource in GetDirectReferencedTypesHelper2(findType, shouldFlatten, includeDerived))
            {
                var type = typeAndSource.TypeVM;

                if (type == findType)
                {
                    // Not interesting to know that you refer to yourself
                    continue;
                }

                if (!type.IsGenericType)
                {
                    yield return typeAndSource;
                }
                else
                {
                    var generic = type.GetGenericTypeDefinition();
                    yield return new TypeAndSource(generic, typeAndSource.Source);

                    foreach (var t in GetTypeArguments(type))
                    {
                        if (t.TypeVM == findType)
                        {
                            // Not interesting to know that you refer to yourself
                            continue;
                        }

                        yield return t;
                    }
                }
            }
        }

        static IEnumerable<TypeAndSource> GetTypeArguments(TypeViewModel type)
        {
            foreach (var t in type.GetGenericArguments())
            {
                if (!t.IsGenericParameter)
                {
                    if (t.IsGenericType)
                    {
                        var generic = t.GetGenericTypeDefinition();
                        yield return new TypeAndSource(generic, "Open type argument: " + generic.Name);

                        foreach (var t2 in GetTypeArguments(t))
                            yield return t2;
                    }
                    else
                    {
                        yield return new TypeAndSource(t, "Type argument: " + t.Name);
                    }
                }
            }
        }

        static internal IEnumerable<TypeAndSource> GetDirectReferencedTypesHelper2(TypeViewModel findType, bool shouldFlatten, bool includeDerived)
        {
            if (includeDerived)
            {
                var baseType = findType.BaseType;
                if (!IsTerminator(baseType))
                {
                    yield return new TypeAndSource(findType.BaseType, "base (" + findType.BaseType.Name + ")");
                }
            }

            foreach (var i in findType.Interfaces) //findType.GetInterfaces())
            {
                yield return new TypeAndSource(i, "Implements: " + i.Name);
            }

            foreach (var property in findType.Properties)
            {
                yield return new TypeAndSource(property.PropertyType, "Property: " + property.Name + " (" + property.PropertyType.Name + ")");
            }

            foreach (var method in findType.Methods)  //type.GetMethods(bindingFlags))
            {
                yield return new TypeAndSource(method.ReturnType, $"Method:  {method.Name} ({method.ReturnType})");


                IList<ParameterViewModel> parameters = null;
                try
                {
                    parameters = method.Parameters;
                }
                catch (Exception)
                {
                    // Work around reflection bug with new type projections
                    parameters = new List<ParameterViewModel>();
                }

                if (parameters != null)
                {
                    foreach (var parameter in parameters)
                    {
                        yield return new TypeAndSource(parameter.ParameterType, $"Parameter: {parameter.Name} on {method.PrettyName} ({parameter.ParameterType}) ");
                    }
                }
            }

            foreach (var constructor in findType.Constructors) 
            {
                foreach (var parameter in constructor.Parameters)
                {
                    yield return new TypeAndSource(parameter.ParameterType, "Constructor parameter: " + parameter.Name + " (" + parameter.ParameterType + ")");
                }
            }

            foreach (var ev in findType.Events)
            {
                yield return new TypeAndSource(ev.EventHandlerType, "Event: " + ev.Name + " (" + ev.EventHandlerType.Name + ")");
            }

            foreach (var field in findType.Fields)
            {
                yield return new TypeAndSource(field.FieldType, "Field: " + field.Name + " (" + field.FieldType.Name + ")");
            }

        }

        static public IEnumerable<TypeViewModel> GetClosureReferencedTypes(TypeViewModel findType)
        {
            var types = new TypeList();
            GetClosureReferencedTypesHelper(findType, types);
            return types;
        }

        static internal void GetClosureReferencedTypesHelper(TypeViewModel findType, TypeList types)
        {
            foreach (var typeAndSource in GetDirectReferencedTypes(findType, true /*shouldFlatten*/, true /* includeDerived */))
            {
                var type = typeAndSource.TypeVM;

                if (type != findType && !types.Contains(type))
                {
                    types.Add(type);

                    if (!IsTerminator(type))
                    {
                        GetClosureReferencedTypesHelper(type, types);
                    }
                }
            }

        }


        static public IEnumerable<TypeViewModel> GetClosureDependentTypes(TypeViewModel findType)
        {
            var dependentTypes = new TypeList();
            var visited = new TypeList();
            var pending = new TypeList();

            foreach (var type in Manager.CurrentTypeSet.Types)
            {
                if (type == findType)
                    continue;

                pending.Add(type);

            }

            GetClosureDependentTypesHelper(findType, dependentTypes, visited, pending);

            return dependentTypes;
        }

        static internal void GetClosureDependentTypesHelper(TypeViewModel findType, TypeList dependentTypes, TypeList visited, TypeList pendingTypes)
        {
            var count = dependentTypes.Count;
            var countChanged = true;

            // Slow. To speed up, build reverse references, and re-use GetClosureDependentTypes
            while (countChanged)
            {
                foreach (var pendingType in pendingTypes)
                {
                    if (dependentTypes.Contains(pendingType))
                        continue;


                    foreach (var typeAndSource in GetDirectReferencedTypes(pendingType, true /*shouldFlatten*/, true /* includeDerived */))
                    {
                        var referencedType = typeAndSource.TypeVM;

                        if (referencedType == findType || dependentTypes.Contains(referencedType))
                        {
                            dependentTypes.Add(pendingType);
                            break;
                        }

                    }

                }

                var newCount = dependentTypes.Count;
                countChanged = newCount != count;
                count = newCount;
            }

            return;
        }


        static public List<List<string>> FindPathsToType(TypeViewModel start, TypeViewModel target)
        {
            var paths = new List<List<string>>();
            FindPathsToTypeHelper(start, target, paths, null, null);
            return paths;
        }

        static internal void FindPathsToTypeHelper(
            TypeViewModel start,
            TypeViewModel target,
            List<List<string>> paths,
            Collection<string> currentPath,
            Collection<TypeViewModel> visited)
        {
            if (visited == null)
                visited = new Collection<TypeViewModel>();

            foreach (var typeAndSource in GetDirectReferencedTypes(start, false /*shouldFlatten*/, true /* includeDerived */))
            {
                var type = typeAndSource.TypeVM;
                var wasVisited = visited.Contains(type);
                visited.Add(type);

                if (currentPath == null)
                    currentPath = new Collection<string>();

                currentPath.Add(typeAndSource.Source);

                if (typeAndSource.TypeVM == target)
                {
                    paths.Add(new List<string>(currentPath));
                }
                else
                {
                    if (!IsTerminator(type) && !wasVisited)
                    {
                        FindPathsToTypeHelper(type, target, paths, currentPath, visited);
                    }
                }
                currentPath.RemoveAt(currentPath.Count - 1);

            }

        }



        static bool IsTypeInType(TypeViewModel tFind, TypeViewModel type)
        {
            if (tFind == type)
                return true;

            if (type.IsGenericType)
            {
                foreach (var ta in type.GetGenericArguments())
                {
                    if (tFind == ta)
                        return true;

                    if (IsTypeInType(tFind, ta))
                        return true;
                }
            }

            return false;
        }

    }
}


#if false
        internal IEnumerable<Type> FindReferencingTypes(Type findType, int referenceIndex)
        {

            var bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            if (referenceIndex != -1 && referenceIndex != _referenceIndex) yield break;

            foreach (var checkType in _currentTypeSet.Types)
            {
                if (!checkType.IsPublic && (!checkType.IsInterface || !Settings.InternalInterfaces))
                    continue;


                bool shouldContinue = false;
                if (referenceIndex != -1 && referenceIndex != _referenceIndex)
                    yield break;

                if (checkType == findType)
                    continue;

                if (IsTypeInType(findType, checkType))
                {
                    yield return checkType;
                    continue;
                }

                foreach (var i in checkType.GetInterfaces())
                {
                    if (IsTypeInType(findType, i))
                    {
                        yield return checkType;
                        shouldContinue = true;
                        break;
                    }

                }
                if (shouldContinue)
                    continue;


                foreach (var property in Type2Properties.GetProperties(checkType, false /*shouldFlatten*/ ))
                {
                    if (IsTypeInType(findType, property.PropertyType))
                    {
                        yield return checkType;
                        shouldContinue = true;
                        break;
                    }
                }
                if (shouldContinue)
                    continue;

                if (findType.Name == "SuspendingEventArgs" && checkType.Name == "SuspendingEventHandler")
                {
                    int i = 0;
                }

                foreach (var method in Type2Methods.GetMethods(checkType, false/*shouldFlatten*/))  //type.GetMethods(bindingFlags))
                {
                    if (IsTypeInType(findType, method.ReturnType))
                    {
                        yield return checkType;
                        shouldContinue = true;
                        break;
                    }
                    if (shouldContinue) break;

                    foreach (var parameter in method.GetParameters())
                    {
                        if (IsTypeInType(findType, parameter.ParameterType))
                        {
                            yield return checkType;
                            shouldContinue = true;
                            break;
                        }
                    }
                    if (shouldContinue) break;
                }
                if (shouldContinue)
                    continue;

                foreach (var constructor in Type2Constructors.GetConstructors(checkType))  //type.GetMethods(bindingFlags))
                {
                    foreach (var parameter in constructor.GetParameters())
                    {
                        if (IsTypeInType(findType, parameter.ParameterType))
                        {
                            yield return checkType;
                            shouldContinue = true;
                            break;
                        }
                    }
                    if (shouldContinue) break;
                }
                if (shouldContinue)
                    continue;

                foreach (var ev in Type2Events.GetEvents(checkType, false /*shouldFlatten*/ ))
                {
                    if (IsTypeInType(findType, ev.EventHandlerType))
                    {
                        yield return checkType;
                        shouldContinue = true;
                        break;
                    }
                }
                if (shouldContinue)
                    continue;

                foreach (var field in Type2Fields.GetFields(checkType, false /*shouldFlatten*/ ))
                {
                    if (IsTypeInType(findType, field.FieldType))
                    {
                        yield return checkType;
                        shouldContinue = true;
                        break;
                    }
                }
                if (shouldContinue)
                    continue;
            }


        }
#endif
