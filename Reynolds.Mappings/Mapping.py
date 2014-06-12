#----
MAX_N = 6
#----

import sys

from CodeGen import *

codegen_begin("Mapping.cs")

stmt("using System")
stmt("using System.Collections")
stmt("using System.Collections.Generic")
stmt("using System.Threading")
stmt("using System.Linq")

with block("namespace Reynolds.Mappings"):
	for n in range(1, MAX_N):
		if n == 1:
			subs = {
				'Tuple<TKeys>': "TKey",
				'TKeys': "TKey",
				'TKeys keys': "TKey key",
				'keytuple': "key",
				'keys': 'key',
				'IEqualityComparer<TKeys>' : "IEqualityComparer<TKey>",
				'IEqualityComparer<TKeys> comparers' : "IEqualityComparer<TKey> comparer"
			}
		else:
			subs = {
				'TKeys': ", ".join(["TKey%i" % (i+1) for i in range(n)]), 
				'TKeys keys': ", ".join(["TKey%i key%i" % (i+1, i+1) for i in range(n)]),
				'Tuple<TKeys>': "Tuple<" + ", ".join(["TKey%i" % (i+1) for i in range(n)]) + ">",
				'keytuple': "new Tuple<" + ", ".join(["TKey%i" % (i+1) for i in range(n)]) + ">(" + ", ".join(["key%i" % (i+1) for i in range(n)]) + ")",
				'keys': ", ".join(['key' + str(i+1) for i in range(n)]),
				'IEqualityComparer<TKeys>' : ", ".join(["IEqualityComparer<TKey%i>" % (i+1) for i in range(n)]),
				'IEqualityComparer<TKeys> comparers' : ", ".join(["IEqualityComparer<TKey%i> comparer%i" % (i+1, i+1) for i in range(n)])
			}
		
		with placeholders(**subs):
			# --------------- IDomain -------------
			with block("public interface IDomain<$TKeys$> : IEnumerable<$Tuple<TKeys>$>"):
				stmt("bool Contains($TKeys keys$)")
				with block("int Count"):
					stmt("get")
				with block("bool IsFinite"):
					stmt("get")
				with block("bool IsNumerable"):
					stmt("get")
					
			# --------------- KeyValueTuple -------------
			with block("public interface IKeyValueTuple<$TKeys$, out TValue>"):
				if n>1:
					for i in range(1, n+1):
						with block("TKey%i Key%i" % (i, i)):
							stmt("get")
				else:
					with block("TKey Key"):
						stmt("get")
				with block("TValue Value"):
					stmt("get")
			with block("public struct KeyValueTuple<$TKeys$, TValue> : IKeyValueTuple<$TKeys$, TValue>"):
				stmt("KeyValuePair<$Tuple<TKeys>$, TValue> inner")
				with block("public KeyValueTuple(KeyValuePair<$Tuple<TKeys>$, TValue> inner)"):
					stmt("this.inner = inner")
				if n>1:
					for i in range(1, n+1):
						with block("public TKey%i Key%i" % (i, i)):
							with block("get"):
								stmt("return inner.Key.Item%i" % i)
				else:
					with block("public TKey Key"):
						with block("get"):
							stmt("return inner.Key")
				with block("public TValue Value"):
					with block("get"):
						stmt("return inner.Value")
			
			# --------------- IMapping ----------------
			with block("public interface IMapping<$TKeys$, out TValue> : IEnumerable<IKeyValueTuple<$TKeys$, TValue>>, IDomain<$TKeys$>"):
				with block("TValue this[$TKeys keys$]"):
					stmt("get")
				stmt("IEnumerator<IKeyValueTuple<$TKeys$, TValue>> GetEnumerator()")

			# --------------- Mapping ----------
			with block("public class Mapping<$TKeys$, TValue> : IMapping<$TKeys$, TValue>"):
				stmt("public delegate TValue GetDelegate($TKeys keys$)")
				stmt("GetDelegate _getter")
				with block("public Mapping(GetDelegate getter)"):
					stmt("_getter = getter")
				with block("public TValue this[$TKeys keys$]"):
					with block("get"):
						stmt("return _getter($keys$)")
				with block("public bool IsFinite"):
					with block("get"):
						stmt("return false")
				with block("public bool IsNumerable"):
					with block("get"):
						stmt("return false")
				with block("public bool Contains($TKeys keys$)"):
					stmt("return true")
				with block("public int Count"):
					with block("get"):
						stmt('throw new Exception("Domain is not finite")')						
				with block("public IEnumerator<IKeyValueTuple<$TKeys$, TValue>> GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("IEnumerator<$Tuple<TKeys>$> IEnumerable<$Tuple<TKeys>$>.GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("IEnumerator IEnumerable.GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
					
		
			# --------------- DictionaryMapping ----------------
			with block("public class DictionaryMapping<$TKeys$, TValue> : Dictionary<$Tuple<TKeys>$, TValue>, IMapping<$TKeys$, TValue>"):
				if n > 1:
					with block("protected class EqualityComparer : IEqualityComparer<$Tuple<TKeys>$>"):
						for i in range(n):
							stmt("IEqualityComparer<TKey%i> comparer%i" % (i+1, i+1))
						with block("public EqualityComparer($IEqualityComparer<TKeys> comparers$)"):
							for i in range(n):
								stmt("this.comparer%i = (comparer%i == null ? EqualityComparer<TKey%i>.Default : comparer%i)" % (i+1, i+1, i+1, i+1))
						with block("public bool Equals($Tuple<TKeys>$ a, $Tuple<TKeys>$ b)"):
							stmt ("return " + " && ".join(("comparer%i.Equals(a.Item%i, b.Item%i)" % (i+1, i+1, i+1)) for i in range(n)))
						with block("public int GetHashCode($Tuple<TKeys>$ obj)"):
							stmt("int result = %i" % n)
							with block("unchecked"):
								for i in range(n):
									stmt("result = result * 23 + comparer%i.GetHashCode(obj.Item%i)" % (i+1, i+1))
							stmt("return result")
				with block("public bool Contains($TKeys keys$)"):
					stmt("return this.ContainsKey($keytuple$)")
				with block("public bool IsFinite"):
					with block("get"):
						stmt("return true")
				with block("public bool IsNumerable"):
					with block("get"):
						stmt("return true")
						
				with block("public DictionaryMapping() : base()"):
					pass
				with block("public DictionaryMapping($IEqualityComparer<TKeys> comparers$) : base (" + ("comparer" if n == 1 else "new EqualityComparer(" + ", ".join(("comparer%i" % (i+1) for i in range(n))) + ")") + ")"):
					pass
				with block("public new IEnumerator<IKeyValueTuple<$TKeys$, TValue>> GetEnumerator()"):
					with block("for(var e = base.GetEnumerator(); e.MoveNext(); )"):
						stmt("yield return new KeyValueTuple<$TKeys$, TValue>(e.Current)")
				with block("IEnumerator<$Tuple<TKeys>$> IEnumerable<$Tuple<TKeys>$>.GetEnumerator()"):
					stmt("return this.Keys.GetEnumerator()")
				with block("IEnumerator IEnumerable.GetEnumerator()"):
					stmt("return this.Keys.GetEnumerator()")				
				if n > 1:
					with block("public bool ContainsKey($TKeys keys$)"):
						stmt("return base.ContainsKey($keytuple$)")
					with block("public bool Remove($TKeys keys$)"):
						stmt("return base.Remove($keytuple$)")
					with block("public void Add($TKeys keys$, TValue value)"):
						stmt("base.Add($keytuple$, value)")
					with block("public bool TryGetValue($TKeys keys$, out TValue value)"):
						stmt("return base.TryGetValue($keytuple$, out value)")
					with block("public TValue this[$TKeys keys$]"):
						with block("get"):
							stmt("return base[$keytuple$]")
						with block("set"):
							stmt("base[$keytuple$] = value")

			# --------------- LazyMapping ----------------
			with block("public class LazyMapping<$TKeys$, TValue> : IMapping<$TKeys$, TValue>"):
				stmt("public delegate TValue InstantiateDelegate($TKeys keys$)")
				stmt("public delegate bool ContainsDelegate($TKeys keys$)")
				stmt("protected InstantiateDelegate _instantiator")
				stmt("protected ContainsDelegate _contains")
				stmt("protected DictionaryMapping<$TKeys$, TValue> _inner")
				with block("public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)"):
					stmt("_inner = new DictionaryMapping<$TKeys$, TValue>()")
					stmt("_instantiator = instantiator")
					stmt("_contains = contains")
				with block("public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, $IEqualityComparer<TKeys> comparers$)"):
					stmt("_inner = new DictionaryMapping<$TKeys$, TValue>(" + ("comparer" if n == 1 else ", ".join(("comparer%i" % (i+1) for i in range(n)))) + ")")
					stmt("_instantiator = instantiator")
					stmt("_contains = contains")
				with block("public bool Contains($TKeys keys$)"):
					with block("if(_contains == null)"):
						stmt("return true")
					with block("else"):
						stmt("return _contains($keys$)")
				with block("public int Count"):
					with block("get"):
						stmt('throw new Exception("Domain is not finite")')
				with block("public bool IsFinite"):
					with block("get"):
						stmt("return false")
				with block("public bool IsNumerable"):
					with block("get"):
						stmt("return false")
				with block("public IEnumerator<IKeyValueTuple<$TKeys$, TValue>> GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("IEnumerator<$Tuple<TKeys>$> IEnumerable<$Tuple<TKeys>$>.GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("IEnumerator IEnumerable.GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("public TValue this[$TKeys keys$]"):
					with block("get"):
						stmt("TValue value")
						with block("if(_inner.TryGetValue($keys$, out value))"):
							stmt("return value")
						with block("else"):
							stmt("_inner[$keys$] = value = _instantiator($keys$)")
							stmt("return value")
				with block("public bool TryGetExisting($TKeys keys$, out TValue value)"):
					stmt("return _inner.TryGetValue($keys$, out value)")
					
			# --------------- WeakLazyMapping ----------------
			with block("public class WeakLazyMapping<$TKeys$, TValue> : WeakMapping, IMapping<$TKeys$, TValue> where TValue : class"):
				stmt("public delegate TValue InstantiateDelegate($TKeys keys$)")
				stmt("public delegate bool ContainsDelegate($TKeys keys$)")
				stmt("protected InstantiateDelegate _instantiator")
				stmt("protected ContainsDelegate _contains")
				stmt("protected DictionaryMapping<$TKeys$, WeakReference> _inner")
				with block("public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)"):
					stmt("_inner = new DictionaryMapping<$TKeys$, WeakReference>()")
					stmt("_instantiator = instantiator")
					stmt("_contains = contains")
					stmt("AddToCleanupList(this)")
				with block("public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, $IEqualityComparer<TKeys> comparers$)"):
					stmt("_inner = new DictionaryMapping<$TKeys$, WeakReference>(" + ("comparer" if n == 1 else ", ".join(("comparer%i" % (i+1) for i in range(n)))) + ")")
					stmt("_instantiator = instantiator")
					stmt("_contains = contains")
				with block("protected override void Cleanup()"):
					stmt("$Tuple<TKeys>$[] keys")
					with block("lock(_inner)"):
						stmt("keys = ((IEnumerable<$Tuple<TKeys>$>) _inner).ToArray()")
					with block("foreach(var key in keys)"):
						with block("lock(_inner)"):
							stmt("WeakReference r");
							stmt("object v");
							with block("if(_inner.TryGetValue(key, out r) && !r.IsAlive)"):
								stmt("_inner.Remove(key)")
				with block("public bool Contains($TKeys keys$)"):
					with block("if(_contains == null)"):
						stmt("return true")
					with block("else"):
						stmt("return _contains($keys$)")
				with block("public int Count"):
					with block("get"):
						stmt('throw new Exception("Domain is not finite")')
				with block("public bool IsFinite"):
					with block("get"):
						stmt("return false")
				with block("public bool IsNumerable"):
					with block("get"):
						stmt("return false")
				with block("public IEnumerator<IKeyValueTuple<$TKeys$, TValue>> GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("IEnumerator<$Tuple<TKeys>$> IEnumerable<$Tuple<TKeys>$>.GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("IEnumerator IEnumerable.GetEnumerator()"):
					stmt('throw new Exception("Domain is non-numerable")')
				with block("public TValue this[$TKeys keys$]"):
					with block("get"):
						stmt("WeakReference r")
						stmt("TValue v")
						with block("lock(_inner)"):
							with block("if(_inner.TryGetValue($keys$, out r))"):
								stmt("v = r.Target as TValue")
								with block("if(v != null)"):
									stmt("return v")
							stmt("_inner[$keys$] = new WeakReference(v = _instantiator($keys$))")
							stmt("return v")
				with block("public bool TryGetExisting($TKeys keys$, out TValue value)"):
					stmt("WeakReference r")
					with block("lock(_inner)"):
						with block("if(_inner.TryGetValue($keys$, out r))"):
							stmt("value = r.Target as TValue")
							stmt("return value != null")
						stmt("value = null")
						stmt("return false")

codegen_end()

# from System import *
# from System.CodeDom import *
# from System.CodeDom.Compiler import *
# from System.IO import *
# from System.Text import *
# from System.Text.RegularExpressions import *
# from Microsoft.CSharp import *
# from TradeStudio.Utils.CodeDom import *

# unit = CodeCompileUnit()

# ns = CodeNamespace("TradeStudio.Utils")
# unit.Namespaces.Add(ns)
# ns.Imports.Add(CodeNamespaceImport("System"))
# ns.Imports.Add(CodeNamespaceImport("System.Collections"))
# ns.Imports.Add(CodeNamespaceImport("System.Collections.Generic"))
# # ns.Imports.Add(CodeNamespaceImport("Trading"))
# # ns.Imports.Add(CodeNamespaceImport("Utils"))
# # ns.Imports.Add(CodeNamespaceImport("Maths"))

# # cs  = CodeTypeDeclaration("Transform")
# # ns.Types.Add(cs)
# # cs.IsClass = True

# this = CodeThisReferenceExpression()

# for n in range(1, MAX_N):
	# if n == 1:
		# Ts = [ "TKey" ]
		# ts = [ "key" ]
		# keytype = Expressions.Type("TKey")
		# tupleexpr = Expressions.Variable("key")
	# else:
		# Ts = [ "TKey" + str(i+1) for i in range(n) ]
		# ts = [ "key" + str(i+1) for i in range(n) ]
		# keytype = Expressions.Type("Tuple", *Ts)
		# tupleexpr = Expressions.Call(Expressions.Variable("Tuple"), "Create", *[Expressions.Variable(t) for t in ts])
	# kvtype = Expressions.Type("KeyValuePair", keytype, "TValue")

	# # --------------- IMapping ----------------
	
	# # interface declaration IMapping<T1, T2, ..., TValue>
	# c = Declare.Interface("IMapping")
	# # c.BaseTypes.Add(Expressions.Type("IEnumerable", kvtype))
	# ns.Types.Add(c)
	
	# # type parameters
	# for T in Ts:
		# c.TypeParameters.Add(T)
	# c.TypeParameters.Add("TValue")
	
	# # item
	# m = Declare.Property(c, MemberAttributes.Public, Expressions.Type("TValue"), "Item");
	# for T, t in zip(Ts, ts):
		# Declare.Parameter(m, T, t)
	# m.HasGet = True
	# m.HasSet = False
	
	# # --------------- IMapping ----------------
	
	# # # interface declaration IMapping<T1, ..., TValue>
	# # c = Declare.Interface("IMapping")
	# # ns.Types.Add(c)

	# # # add IMapping<T1, ..., TValue> implementation
	# # c.BaseTypes.Add(Expressions.Type("IMapping", *(Ts + ["TValue"])))
	
	# # # type parameters
	# # for T in Ts:
		# # c.TypeParameters.Add(T)
	# # c.TypeParameters.Add("TValue")
	
	# # # remove
	# # m = Declare.Method(c, MemberAttributes.Public, Types.Bool, "Remove");
	# # for T, t in zip(Ts, ts):
		# # Declare.Parameter(m, T, t)
		
	# # # add
	# # m = Declare.Method(c, MemberAttributes.Public, Types.Void, "Add");
	# # for T, t in zip(Ts, ts):
		# # Declare.Parameter(m, T, t)
	# # Declare.Parameter(m, "TValue", "value")
	
	# # # item
	# # m = Declare.Property(c, MemberAttributes.Public | MemberAttributes.New, Expressions.Type("TValue"), "Item");
	# # for T, t in zip(Ts, ts):
		# # Declare.Parameter(m, T, t)
	# # m.HasGet = m.HasSet = True

	# # --------------- Mapping ----------------
	
	# # class
	# c = Declare.Class("Mapping")
	# ns.Types.Add(c)

	# if n == 1:
		# c.BaseTypes.Add(Expressions.Type("Dictionary", Ts[0], "TValue"))
	# else:
		# c.BaseTypes.Add(Expressions.Type("Dictionary", keytype, "TValue"))
	# c.BaseTypes.Add(Expressions.Type("IMapping", *(Ts + ["TValue"])))
	
	# for T in Ts:
		# c.TypeParameters.Add(T)
	# c.TypeParameters.Add("TValue")
	
	# if n > 1:
		# m = Declare.Method(c, MemberAttributes.Public, Types.Bool, "ContainsKey");
		# for T, t in zip(Ts, ts):
			# Declare.Parameter(m, T, t)
		# m.Statements.Add(Statements.Return(Expressions.Call(None, "ContainsKey", Expressions.New(keytype, *([Expressions.Variable(t) for t in ts])))))

		# m = Declare.Method(c, MemberAttributes.Public, Types.Bool, "Remove");
		# for T, t in zip(Ts, ts):
			# Declare.Parameter(m, T, t)
		# m.Statements.Add(Statements.Return(Expressions.Call(None, "Remove", Expressions.New(keytype, *([Expressions.Variable(t) for t in ts])))))
		
		# m = Declare.Method(c, MemberAttributes.Public, Types.Void, "Add");
		# for T, t in zip(Ts, ts):
			# Declare.Parameter(m, T, t)
		# Declare.Parameter(m, "TValue", "value")
		# m.Statements.Add(Expressions.Call(None, "Add", Expressions.New(keytype, *([Expressions.Variable(t) for t in ts])), Expressions.Variable("value")))
	
		# m = Declare.Method(c, MemberAttributes.Public, Types.Bool, "TryGetValue");
		# for T, t in zip(Ts, ts):
			# Declare.Parameter(m, T, t)
		# Declare.Parameter(m, "TValue", "value", FieldDirection.Out)	
		# m.Statements.Add(Statements.Return(Expressions.Call(None, "TryGetValue", Expressions.New(keytype, *([Expressions.Variable(t) for t in ts])), Expressions.Variable("out value"))))
		
		# m = Declare.Property(c, MemberAttributes.Public, Expressions.Type("TValue"), "Item")
		# for T, t in zip(Ts, ts):
			# Declare.Parameter(m, T, t)
		# m.GetStatements.Add(Statements.Return(Expressions.Index(this, Expressions.New(keytype, *([Expressions.Variable(t) for t in ts])))))
		# m.SetStatements.Add(Statements.Assign(Expressions.Index(this, Expressions.New(keytype, *([Expressions.Variable(t) for t in ts]))), Expressions.Variable("value")))

	# # --------------- LazyMapping ----------------

	# # lazy class
	# c = Declare.Class("LazyMapping")
	# ns.Types.Add(c)

	# c.BaseTypes.Add(Expressions.Type("IMapping", *(Ts + ["TValue"])))
	
	# for T in Ts:
		# c.TypeParameters.Add(T)
	# c.TypeParameters.Add("TValue")
	
	# d = Declare.Delegate(Expressions.Type("TValue"), "InstantiateDelegate");
	# for T, t in zip(Ts, ts):
		# Declare.Parameter(d, T, t)
	# c.Members.Add(d)

	# Declare.Field(c, MemberAttributes.Family, Expressions.Type("InstantiateDelegate"), "_instantiator")
	# Declare.Field(c, MemberAttributes.Family, Expressions.Type("Mapping", *(Ts + ["TValue"])), "_inner")
	
	# # constructor
	# cc = CodeConstructor()
	# c.Members.Add(cc)
	# cc.Attributes = MemberAttributes.Public
	# Declare.Parameter(cc, "InstantiateDelegate", "_instantiator");
	# cc.Statements.Add(Statements.Assign(Expressions.Variable("_inner"), Expressions.New(Expressions.Type("Mapping", *(Ts + ["TValue"])))))

	# # this[...]
	# m = Declare.Property(c, MemberAttributes.Public, Expressions.Type("TValue"), "Item");
	# for T, t in zip(Ts, ts):
		# Declare.Parameter(m, T, t)
	# m.GetStatements.Add(Statements.Declare(Expressions.Type("TValue"), "value"))
	# m.GetStatements.Add(Statements.If(Expressions.Call(Expressions.Variable("_inner"), "TryGetValue", *([Expressions.Variable(t) for t in (ts + ["out value"])])),
		# Statements.Return(Expressions.Variable("value")),
		# # Statements.If(Expressions.Or(Expressions.Snippet("AdmissibleKeys == null"), Expressions.Call(Expressions.Variable("AdmissibleKeys"), "Contains", tupleexpr)),
			# Statements.Block(
				# Statements.Assign(Expressions.Variable("value"), Expressions.Call(None, "_instantiator", *([Expressions.Variable(t) for t in ts]))),
				# Statements.Call(Expressions.Variable("_inner"), "Add", *([Expressions.Variable(t) for t in (ts + ["value"])])),
				# Statements.Return(Expressions.Variable("value"))
			# ) #,
			# # Statements.Throw(Expressions.New(Expressions.Type("KeyNotFoundException")))
	# #	)
	# ));
	
	# # --------------- WeakLazyMapping ----------------

	# # lazy class
	# c = Declare.Class("WeakLazyMapping")
	# ns.Types.Add(c)
	# c.BaseTypes.Add(Expressions.Type("IMapping", *(Ts + ["TValue"])))
	
	# for T in Ts:
		# c.TypeParameters.Add(T)
	# c.TypeParameters.Add("TValue")
	# c.TypeParameters[c.TypeParameters.Count - 1].Constraints.Add(" class")
	
	# d = Declare.Delegate(Expressions.Type("TValue"), "InstantiateDelegate");
	# for T, t in zip(Ts, ts):
		# Declare.Parameter(d, T, t)
	# c.Members.Add(d)

	# Declare.Field(c, MemberAttributes.Family, Expressions.Type("InstantiateDelegate"), "_instantiator")
	# Declare.Field(c, MemberAttributes.Family, Expressions.Type("Mapping", *(Ts + ["WeakReference"])), "_inner")
	
	# # constructor
	# cc = CodeConstructor()
	# c.Members.Add(cc)
	# cc.Attributes = MemberAttributes.Public
	# Declare.Parameter(cc, "InstantiateDelegate", "_instantiator");
	# cc.Statements.Add(Statements.Assign(Expressions.Variable("_inner"), Expressions.New(Expressions.Type("Mapping", *(Ts + ["WeakReference"])))))
	# cc.Statements.Add(

	# # this[...]
	# m = Declare.Property(c, MemberAttributes.Public, Expressions.Type("TValue"), "Item");
	# for T, t in zip(Ts, ts):
		# Declare.Parameter(m, T, t)
	# m.GetStatements.Add(Statements.Declare(Expressions.Type("WeakReference"), "reference"))
	# m.GetStatements.Add(Statements.Declare(Expressions.Type("TValue"), "value"))
	# m.GetStatements.Add(
		# Statements.If(Expressions.Call(Expressions.Variable("_inner"), "TryGetValue", *([Expressions.Variable(t) for t in (ts + ["out reference"])])),
			# Statements.Block(
				# Statements.Assign(Expressions.Variable("value"), Expressions.Snippet("reference.Target as TValue")),
				# Statements.If(Expressions.Snippet("value != null"),
					# Statements.Return(Expressions.Variable("value")),
					# Statements.Block()
				# )
			# ),
			# Statements.Block()
		# )
	# )
	
	# m.GetStatements.Add(
		# Statements.Assign(Expressions.Variable("value"), Expressions.Call(None, "_instantiator", *([Expressions.Variable(t) for t in ts])))
	# )
	
	# m.GetStatements.Add(
		# Statements.Assign(Expressions.Snippet("_inner[" + ", ".join(ts) + "]"), Expressions.Snippet("new WeakReference(value)"))
	# )
	
	# m.GetStatements.Add(
		# Statements.Return(Expressions.Variable("value"))
	# )
	
	# # Clean
	# m = Declare.Method(c, MemberAttributes.Family, Types.Void, "Clean")
	# m.Statements.Add(Statements.Snippet("foreach(var kv in _inner) if(kv.Value.Target == null) _inner.Remove(kv.Key);"));
	
# sb = StringBuilder()
# cgu = CodeGeneratorOptions()
# cgu.IndentString = "\t"
# cgu.BracingStyle = "C"
# CSharpCodeProvider().GenerateCodeFromCompileUnit(unit, StringWriter(sb), cgu)

# s = sb.ToString()
# # remove empty whitespace before {
# s = Regex(r"\r\n((\t*)\r\n)*(?<indent>(\t)*){").Replace(s, "\r\n${indent}{")
# # remove empty whitespace after {
# s = Regex(r"{\r\n((\t)*\r\n)*").Replace(s, "{\r\n")

# #print s

# sw = StreamWriter("Mapping.cs")
# sw.Write(s)
# sw.Close()