using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
namespace Reynolds.Mappings
{
	public interface IDomain<TKey> : IEnumerable<TKey>
	{
		bool Contains(TKey key);
		int Count
		{
			get;
		}
		bool IsFinite
		{
			get;
		}
		bool IsNumerable
		{
			get;
		}
	}
	public interface IKeyValueTuple<TKey, out TValue>
	{
		TKey Key
		{
			get;
		}
		TValue Value
		{
			get;
		}
	}
	public struct KeyValueTuple<TKey, TValue> : IKeyValueTuple<TKey, TValue>
	{
		KeyValuePair<TKey, TValue> inner;
		public KeyValueTuple(KeyValuePair<TKey, TValue> inner)
		{
			this.inner = inner;
		}
		public TKey Key
		{
			get
			{
				return inner.Key;
			}
		}
		public TValue Value
		{
			get
			{
				return inner.Value;
			}
		}
	}
	public interface IMapping<TKey, out TValue> : IEnumerable<IKeyValueTuple<TKey, TValue>>, IDomain<TKey>
	{
		TValue this[TKey key]
		{
			get;
		}
		IEnumerator<IKeyValueTuple<TKey, TValue>> GetEnumerator();
	}
	public class Mapping<TKey, TValue> : IMapping<TKey, TValue>
	{
		public delegate TValue GetDelegate(TKey key);
		GetDelegate _getter;
		public Mapping(GetDelegate getter)
		{
			_getter = getter;
		}
		public TValue this[TKey key]
		{
			get
			{
				return _getter(key);
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public bool Contains(TKey key)
		{
			return true;
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public IEnumerator<IKeyValueTuple<TKey, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
	}
	public class DictionaryMapping<TKey, TValue> : Dictionary<TKey, TValue>, IMapping<TKey, TValue>
	{
		public bool Contains(TKey key)
		{
			return this.ContainsKey(key);
		}
		public bool IsFinite
		{
			get
			{
				return true;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return true;
			}
		}
		public DictionaryMapping() : base()
		{
		}
		public DictionaryMapping(IEqualityComparer<TKey> comparer) : base (comparer)
		{
		}
		public new IEnumerator<IKeyValueTuple<TKey, TValue>> GetEnumerator()
		{
			for(var e = base.GetEnumerator(); e.MoveNext(); )
			{
				yield return new KeyValueTuple<TKey, TValue>(e.Current);
			}
		}
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
	}
	public class LazyMapping<TKey, TValue> : IMapping<TKey, TValue>
	{
		public delegate TValue InstantiateDelegate(TKey key);
		public delegate bool ContainsDelegate(TKey key);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey, TValue> _inner;
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey, TValue>();
			_instantiator = instantiator;
			_contains = contains;
		}
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey> comparer)
		{
			_inner = new DictionaryMapping<TKey, TValue>(comparer);
			_instantiator = instantiator;
			_contains = contains;
		}
		public bool Contains(TKey key)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey key]
		{
			get
			{
				TValue value;
				if(_inner.TryGetValue(key, out value))
				{
					return value;
				}
				else
				{
					_inner[key] = value = _instantiator(key);
					return value;
				}
			}
		}
		public bool TryGetExisting(TKey key, out TValue value)
		{
			return _inner.TryGetValue(key, out value);
		}
	}
	public class WeakLazyMapping<TKey, TValue> : WeakMapping, IMapping<TKey, TValue> where TValue : class
	{
		public delegate TValue InstantiateDelegate(TKey key);
		public delegate bool ContainsDelegate(TKey key);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey, WeakReference> _inner;
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey, WeakReference>();
			_instantiator = instantiator;
			_contains = contains;
			AddToCleanupList(this);
		}
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey> comparer)
		{
			_inner = new DictionaryMapping<TKey, WeakReference>(comparer);
			_instantiator = instantiator;
			_contains = contains;
		}
		protected override void Cleanup()
		{
			TKey[] keys;
			lock(_inner)
			{
				keys = ((IEnumerable<TKey>) _inner).ToArray();
			}
			foreach(var key in keys)
			{
				lock(_inner)
				{
					WeakReference r;
					object v;
					if(_inner.TryGetValue(key, out r) && !r.IsAlive)
					{
						_inner.Remove(key);
					}
				}
			}
		}
		public bool Contains(TKey key)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey key]
		{
			get
			{
				WeakReference r;
				TValue v;
				lock(_inner)
				{
					if(_inner.TryGetValue(key, out r))
					{
						v = r.Target as TValue;
						if(v != null)
						{
							return v;
						}
					}
					_inner[key] = new WeakReference(v = _instantiator(key));
					return v;
				}
			}
		}
		public bool TryGetExisting(TKey key, out TValue value)
		{
			WeakReference r;
			lock(_inner)
			{
				if(_inner.TryGetValue(key, out r))
				{
					value = r.Target as TValue;
					return value != null;
				}
				value = null;
				return false;
			}
		}
	}
	public interface IDomain<TKey1, TKey2> : IEnumerable<Tuple<TKey1, TKey2>>
	{
		bool Contains(TKey1 key1, TKey2 key2);
		int Count
		{
			get;
		}
		bool IsFinite
		{
			get;
		}
		bool IsNumerable
		{
			get;
		}
	}
	public interface IKeyValueTuple<TKey1, TKey2, out TValue>
	{
		TKey1 Key1
		{
			get;
		}
		TKey2 Key2
		{
			get;
		}
		TValue Value
		{
			get;
		}
	}
	public struct KeyValueTuple<TKey1, TKey2, TValue> : IKeyValueTuple<TKey1, TKey2, TValue>
	{
		KeyValuePair<Tuple<TKey1, TKey2>, TValue> inner;
		public KeyValueTuple(KeyValuePair<Tuple<TKey1, TKey2>, TValue> inner)
		{
			this.inner = inner;
		}
		public TKey1 Key1
		{
			get
			{
				return inner.Key.Item1;
			}
		}
		public TKey2 Key2
		{
			get
			{
				return inner.Key.Item2;
			}
		}
		public TValue Value
		{
			get
			{
				return inner.Value;
			}
		}
	}
	public interface IMapping<TKey1, TKey2, out TValue> : IEnumerable<IKeyValueTuple<TKey1, TKey2, TValue>>, IDomain<TKey1, TKey2>
	{
		TValue this[TKey1 key1, TKey2 key2]
		{
			get;
		}
		IEnumerator<IKeyValueTuple<TKey1, TKey2, TValue>> GetEnumerator();
	}
	public class Mapping<TKey1, TKey2, TValue> : IMapping<TKey1, TKey2, TValue>
	{
		public delegate TValue GetDelegate(TKey1 key1, TKey2 key2);
		GetDelegate _getter;
		public Mapping(GetDelegate getter)
		{
			_getter = getter;
		}
		public TValue this[TKey1 key1, TKey2 key2]
		{
			get
			{
				return _getter(key1, key2);
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2)
		{
			return true;
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2>> IEnumerable<Tuple<TKey1, TKey2>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
	}
	public class DictionaryMapping<TKey1, TKey2, TValue> : Dictionary<Tuple<TKey1, TKey2>, TValue>, IMapping<TKey1, TKey2, TValue>
	{
		protected class EqualityComparer : IEqualityComparer<Tuple<TKey1, TKey2>>
		{
			IEqualityComparer<TKey1> comparer1;
			IEqualityComparer<TKey2> comparer2;
			public EqualityComparer(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2)
			{
				this.comparer1 = (comparer1 == null ? EqualityComparer<TKey1>.Default : comparer1);
				this.comparer2 = (comparer2 == null ? EqualityComparer<TKey2>.Default : comparer2);
			}
			public bool Equals(Tuple<TKey1, TKey2> a, Tuple<TKey1, TKey2> b)
			{
				return comparer1.Equals(a.Item1, b.Item1) && comparer2.Equals(a.Item2, b.Item2);
			}
			public int GetHashCode(Tuple<TKey1, TKey2> obj)
			{
				int result = 2;
				unchecked
				{
					result = result * 23 + comparer1.GetHashCode(obj.Item1);
					result = result * 23 + comparer2.GetHashCode(obj.Item2);
				}
				return result;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2)
		{
			return this.ContainsKey(new Tuple<TKey1, TKey2>(key1, key2));
		}
		public bool IsFinite
		{
			get
			{
				return true;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return true;
			}
		}
		public DictionaryMapping() : base()
		{
		}
		public DictionaryMapping(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2) : base (new EqualityComparer(comparer1, comparer2))
		{
		}
		public new IEnumerator<IKeyValueTuple<TKey1, TKey2, TValue>> GetEnumerator()
		{
			for(var e = base.GetEnumerator(); e.MoveNext(); )
			{
				yield return new KeyValueTuple<TKey1, TKey2, TValue>(e.Current);
			}
		}
		IEnumerator<Tuple<TKey1, TKey2>> IEnumerable<Tuple<TKey1, TKey2>>.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		public bool ContainsKey(TKey1 key1, TKey2 key2)
		{
			return base.ContainsKey(new Tuple<TKey1, TKey2>(key1, key2));
		}
		public bool Remove(TKey1 key1, TKey2 key2)
		{
			return base.Remove(new Tuple<TKey1, TKey2>(key1, key2));
		}
		public void Add(TKey1 key1, TKey2 key2, TValue value)
		{
			base.Add(new Tuple<TKey1, TKey2>(key1, key2), value);
		}
		public bool TryGetValue(TKey1 key1, TKey2 key2, out TValue value)
		{
			return base.TryGetValue(new Tuple<TKey1, TKey2>(key1, key2), out value);
		}
		public TValue this[TKey1 key1, TKey2 key2]
		{
			get
			{
				return base[new Tuple<TKey1, TKey2>(key1, key2)];
			}
			set
			{
				base[new Tuple<TKey1, TKey2>(key1, key2)] = value;
			}
		}
	}
	public class LazyMapping<TKey1, TKey2, TValue> : IMapping<TKey1, TKey2, TValue>
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, TValue> _inner;
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TValue>();
			_instantiator = instantiator;
			_contains = contains;
		}
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TValue>(comparer1, comparer2);
			_instantiator = instantiator;
			_contains = contains;
		}
		public bool Contains(TKey1 key1, TKey2 key2)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2>> IEnumerable<Tuple<TKey1, TKey2>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2]
		{
			get
			{
				TValue value;
				if(_inner.TryGetValue(key1, key2, out value))
				{
					return value;
				}
				else
				{
					_inner[key1, key2] = value = _instantiator(key1, key2);
					return value;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, out TValue value)
		{
			return _inner.TryGetValue(key1, key2, out value);
		}
	}
	public class WeakLazyMapping<TKey1, TKey2, TValue> : WeakMapping, IMapping<TKey1, TKey2, TValue> where TValue : class
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, WeakReference> _inner;
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, WeakReference>();
			_instantiator = instantiator;
			_contains = contains;
			AddToCleanupList(this);
		}
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, WeakReference>(comparer1, comparer2);
			_instantiator = instantiator;
			_contains = contains;
		}
		protected override void Cleanup()
		{
			Tuple<TKey1, TKey2>[] keys;
			lock(_inner)
			{
				keys = ((IEnumerable<Tuple<TKey1, TKey2>>) _inner).ToArray();
			}
			foreach(var key in keys)
			{
				lock(_inner)
				{
					WeakReference r;
					object v;
					if(_inner.TryGetValue(key, out r) && !r.IsAlive)
					{
						_inner.Remove(key);
					}
				}
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2>> IEnumerable<Tuple<TKey1, TKey2>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2]
		{
			get
			{
				WeakReference r;
				TValue v;
				lock(_inner)
				{
					if(_inner.TryGetValue(key1, key2, out r))
					{
						v = r.Target as TValue;
						if(v != null)
						{
							return v;
						}
					}
					_inner[key1, key2] = new WeakReference(v = _instantiator(key1, key2));
					return v;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, out TValue value)
		{
			WeakReference r;
			lock(_inner)
			{
				if(_inner.TryGetValue(key1, key2, out r))
				{
					value = r.Target as TValue;
					return value != null;
				}
				value = null;
				return false;
			}
		}
	}
	public interface IDomain<TKey1, TKey2, TKey3> : IEnumerable<Tuple<TKey1, TKey2, TKey3>>
	{
		bool Contains(TKey1 key1, TKey2 key2, TKey3 key3);
		int Count
		{
			get;
		}
		bool IsFinite
		{
			get;
		}
		bool IsNumerable
		{
			get;
		}
	}
	public interface IKeyValueTuple<TKey1, TKey2, TKey3, out TValue>
	{
		TKey1 Key1
		{
			get;
		}
		TKey2 Key2
		{
			get;
		}
		TKey3 Key3
		{
			get;
		}
		TValue Value
		{
			get;
		}
	}
	public struct KeyValueTuple<TKey1, TKey2, TKey3, TValue> : IKeyValueTuple<TKey1, TKey2, TKey3, TValue>
	{
		KeyValuePair<Tuple<TKey1, TKey2, TKey3>, TValue> inner;
		public KeyValueTuple(KeyValuePair<Tuple<TKey1, TKey2, TKey3>, TValue> inner)
		{
			this.inner = inner;
		}
		public TKey1 Key1
		{
			get
			{
				return inner.Key.Item1;
			}
		}
		public TKey2 Key2
		{
			get
			{
				return inner.Key.Item2;
			}
		}
		public TKey3 Key3
		{
			get
			{
				return inner.Key.Item3;
			}
		}
		public TValue Value
		{
			get
			{
				return inner.Value;
			}
		}
	}
	public interface IMapping<TKey1, TKey2, TKey3, out TValue> : IEnumerable<IKeyValueTuple<TKey1, TKey2, TKey3, TValue>>, IDomain<TKey1, TKey2, TKey3>
	{
		TValue this[TKey1 key1, TKey2 key2, TKey3 key3]
		{
			get;
		}
		IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TValue>> GetEnumerator();
	}
	public class Mapping<TKey1, TKey2, TKey3, TValue> : IMapping<TKey1, TKey2, TKey3, TValue>
	{
		public delegate TValue GetDelegate(TKey1 key1, TKey2 key2, TKey3 key3);
		GetDelegate _getter;
		public Mapping(GetDelegate getter)
		{
			_getter = getter;
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3]
		{
			get
			{
				return _getter(key1, key2, key3);
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			return true;
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3>> IEnumerable<Tuple<TKey1, TKey2, TKey3>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
	}
	public class DictionaryMapping<TKey1, TKey2, TKey3, TValue> : Dictionary<Tuple<TKey1, TKey2, TKey3>, TValue>, IMapping<TKey1, TKey2, TKey3, TValue>
	{
		protected class EqualityComparer : IEqualityComparer<Tuple<TKey1, TKey2, TKey3>>
		{
			IEqualityComparer<TKey1> comparer1;
			IEqualityComparer<TKey2> comparer2;
			IEqualityComparer<TKey3> comparer3;
			public EqualityComparer(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3)
			{
				this.comparer1 = (comparer1 == null ? EqualityComparer<TKey1>.Default : comparer1);
				this.comparer2 = (comparer2 == null ? EqualityComparer<TKey2>.Default : comparer2);
				this.comparer3 = (comparer3 == null ? EqualityComparer<TKey3>.Default : comparer3);
			}
			public bool Equals(Tuple<TKey1, TKey2, TKey3> a, Tuple<TKey1, TKey2, TKey3> b)
			{
				return comparer1.Equals(a.Item1, b.Item1) && comparer2.Equals(a.Item2, b.Item2) && comparer3.Equals(a.Item3, b.Item3);
			}
			public int GetHashCode(Tuple<TKey1, TKey2, TKey3> obj)
			{
				int result = 3;
				unchecked
				{
					result = result * 23 + comparer1.GetHashCode(obj.Item1);
					result = result * 23 + comparer2.GetHashCode(obj.Item2);
					result = result * 23 + comparer3.GetHashCode(obj.Item3);
				}
				return result;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			return this.ContainsKey(new Tuple<TKey1, TKey2, TKey3>(key1, key2, key3));
		}
		public bool IsFinite
		{
			get
			{
				return true;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return true;
			}
		}
		public DictionaryMapping() : base()
		{
		}
		public DictionaryMapping(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3) : base (new EqualityComparer(comparer1, comparer2, comparer3))
		{
		}
		public new IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TValue>> GetEnumerator()
		{
			for(var e = base.GetEnumerator(); e.MoveNext(); )
			{
				yield return new KeyValueTuple<TKey1, TKey2, TKey3, TValue>(e.Current);
			}
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3>> IEnumerable<Tuple<TKey1, TKey2, TKey3>>.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		public bool ContainsKey(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			return base.ContainsKey(new Tuple<TKey1, TKey2, TKey3>(key1, key2, key3));
		}
		public bool Remove(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			return base.Remove(new Tuple<TKey1, TKey2, TKey3>(key1, key2, key3));
		}
		public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TValue value)
		{
			base.Add(new Tuple<TKey1, TKey2, TKey3>(key1, key2, key3), value);
		}
		public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, out TValue value)
		{
			return base.TryGetValue(new Tuple<TKey1, TKey2, TKey3>(key1, key2, key3), out value);
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3]
		{
			get
			{
				return base[new Tuple<TKey1, TKey2, TKey3>(key1, key2, key3)];
			}
			set
			{
				base[new Tuple<TKey1, TKey2, TKey3>(key1, key2, key3)] = value;
			}
		}
	}
	public class LazyMapping<TKey1, TKey2, TKey3, TValue> : IMapping<TKey1, TKey2, TKey3, TValue>
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2, TKey3 key3);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2, TKey3 key3);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, TKey3, TValue> _inner;
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TValue>();
			_instantiator = instantiator;
			_contains = contains;
		}
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TValue>(comparer1, comparer2, comparer3);
			_instantiator = instantiator;
			_contains = contains;
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2, key3);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3>> IEnumerable<Tuple<TKey1, TKey2, TKey3>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3]
		{
			get
			{
				TValue value;
				if(_inner.TryGetValue(key1, key2, key3, out value))
				{
					return value;
				}
				else
				{
					_inner[key1, key2, key3] = value = _instantiator(key1, key2, key3);
					return value;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, TKey3 key3, out TValue value)
		{
			return _inner.TryGetValue(key1, key2, key3, out value);
		}
	}
	public class WeakLazyMapping<TKey1, TKey2, TKey3, TValue> : WeakMapping, IMapping<TKey1, TKey2, TKey3, TValue> where TValue : class
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2, TKey3 key3);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2, TKey3 key3);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, TKey3, WeakReference> _inner;
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, WeakReference>();
			_instantiator = instantiator;
			_contains = contains;
			AddToCleanupList(this);
		}
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, WeakReference>(comparer1, comparer2, comparer3);
			_instantiator = instantiator;
			_contains = contains;
		}
		protected override void Cleanup()
		{
			Tuple<TKey1, TKey2, TKey3>[] keys;
			lock(_inner)
			{
				keys = ((IEnumerable<Tuple<TKey1, TKey2, TKey3>>) _inner).ToArray();
			}
			foreach(var key in keys)
			{
				lock(_inner)
				{
					WeakReference r;
					object v;
					if(_inner.TryGetValue(key, out r) && !r.IsAlive)
					{
						_inner.Remove(key);
					}
				}
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2, key3);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3>> IEnumerable<Tuple<TKey1, TKey2, TKey3>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3]
		{
			get
			{
				WeakReference r;
				TValue v;
				lock(_inner)
				{
					if(_inner.TryGetValue(key1, key2, key3, out r))
					{
						v = r.Target as TValue;
						if(v != null)
						{
							return v;
						}
					}
					_inner[key1, key2, key3] = new WeakReference(v = _instantiator(key1, key2, key3));
					return v;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, TKey3 key3, out TValue value)
		{
			WeakReference r;
			lock(_inner)
			{
				if(_inner.TryGetValue(key1, key2, key3, out r))
				{
					value = r.Target as TValue;
					return value != null;
				}
				value = null;
				return false;
			}
		}
	}
	public interface IDomain<TKey1, TKey2, TKey3, TKey4> : IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4>>
	{
		bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4);
		int Count
		{
			get;
		}
		bool IsFinite
		{
			get;
		}
		bool IsNumerable
		{
			get;
		}
	}
	public interface IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, out TValue>
	{
		TKey1 Key1
		{
			get;
		}
		TKey2 Key2
		{
			get;
		}
		TKey3 Key3
		{
			get;
		}
		TKey4 Key4
		{
			get;
		}
		TValue Value
		{
			get;
		}
	}
	public struct KeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue> : IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>
	{
		KeyValuePair<Tuple<TKey1, TKey2, TKey3, TKey4>, TValue> inner;
		public KeyValueTuple(KeyValuePair<Tuple<TKey1, TKey2, TKey3, TKey4>, TValue> inner)
		{
			this.inner = inner;
		}
		public TKey1 Key1
		{
			get
			{
				return inner.Key.Item1;
			}
		}
		public TKey2 Key2
		{
			get
			{
				return inner.Key.Item2;
			}
		}
		public TKey3 Key3
		{
			get
			{
				return inner.Key.Item3;
			}
		}
		public TKey4 Key4
		{
			get
			{
				return inner.Key.Item4;
			}
		}
		public TValue Value
		{
			get
			{
				return inner.Value;
			}
		}
	}
	public interface IMapping<TKey1, TKey2, TKey3, TKey4, out TValue> : IEnumerable<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>>, IDomain<TKey1, TKey2, TKey3, TKey4>
	{
		TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4]
		{
			get;
		}
		IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>> GetEnumerator();
	}
	public class Mapping<TKey1, TKey2, TKey3, TKey4, TValue> : IMapping<TKey1, TKey2, TKey3, TKey4, TValue>
	{
		public delegate TValue GetDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4);
		GetDelegate _getter;
		public Mapping(GetDelegate getter)
		{
			_getter = getter;
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4]
		{
			get
			{
				return _getter(key1, key2, key3, key4);
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4)
		{
			return true;
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
	}
	public class DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TValue> : Dictionary<Tuple<TKey1, TKey2, TKey3, TKey4>, TValue>, IMapping<TKey1, TKey2, TKey3, TKey4, TValue>
	{
		protected class EqualityComparer : IEqualityComparer<Tuple<TKey1, TKey2, TKey3, TKey4>>
		{
			IEqualityComparer<TKey1> comparer1;
			IEqualityComparer<TKey2> comparer2;
			IEqualityComparer<TKey3> comparer3;
			IEqualityComparer<TKey4> comparer4;
			public EqualityComparer(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4)
			{
				this.comparer1 = (comparer1 == null ? EqualityComparer<TKey1>.Default : comparer1);
				this.comparer2 = (comparer2 == null ? EqualityComparer<TKey2>.Default : comparer2);
				this.comparer3 = (comparer3 == null ? EqualityComparer<TKey3>.Default : comparer3);
				this.comparer4 = (comparer4 == null ? EqualityComparer<TKey4>.Default : comparer4);
			}
			public bool Equals(Tuple<TKey1, TKey2, TKey3, TKey4> a, Tuple<TKey1, TKey2, TKey3, TKey4> b)
			{
				return comparer1.Equals(a.Item1, b.Item1) && comparer2.Equals(a.Item2, b.Item2) && comparer3.Equals(a.Item3, b.Item3) && comparer4.Equals(a.Item4, b.Item4);
			}
			public int GetHashCode(Tuple<TKey1, TKey2, TKey3, TKey4> obj)
			{
				int result = 4;
				unchecked
				{
					result = result * 23 + comparer1.GetHashCode(obj.Item1);
					result = result * 23 + comparer2.GetHashCode(obj.Item2);
					result = result * 23 + comparer3.GetHashCode(obj.Item3);
					result = result * 23 + comparer4.GetHashCode(obj.Item4);
				}
				return result;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4)
		{
			return this.ContainsKey(new Tuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4));
		}
		public bool IsFinite
		{
			get
			{
				return true;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return true;
			}
		}
		public DictionaryMapping() : base()
		{
		}
		public DictionaryMapping(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4) : base (new EqualityComparer(comparer1, comparer2, comparer3, comparer4))
		{
		}
		public new IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>> GetEnumerator()
		{
			for(var e = base.GetEnumerator(); e.MoveNext(); )
			{
				yield return new KeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>(e.Current);
			}
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4>>.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		public bool ContainsKey(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4)
		{
			return base.ContainsKey(new Tuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4));
		}
		public bool Remove(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4)
		{
			return base.Remove(new Tuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4));
		}
		public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TValue value)
		{
			base.Add(new Tuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4), value);
		}
		public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, out TValue value)
		{
			return base.TryGetValue(new Tuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4), out value);
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4]
		{
			get
			{
				return base[new Tuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4)];
			}
			set
			{
				base[new Tuple<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4)] = value;
			}
		}
	}
	public class LazyMapping<TKey1, TKey2, TKey3, TKey4, TValue> : IMapping<TKey1, TKey2, TKey3, TKey4, TValue>
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TValue> _inner;
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TValue>();
			_instantiator = instantiator;
			_contains = contains;
		}
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TValue>(comparer1, comparer2, comparer3, comparer4);
			_instantiator = instantiator;
			_contains = contains;
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2, key3, key4);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4]
		{
			get
			{
				TValue value;
				if(_inner.TryGetValue(key1, key2, key3, key4, out value))
				{
					return value;
				}
				else
				{
					_inner[key1, key2, key3, key4] = value = _instantiator(key1, key2, key3, key4);
					return value;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, out TValue value)
		{
			return _inner.TryGetValue(key1, key2, key3, key4, out value);
		}
	}
	public class WeakLazyMapping<TKey1, TKey2, TKey3, TKey4, TValue> : WeakMapping, IMapping<TKey1, TKey2, TKey3, TKey4, TValue> where TValue : class
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, TKey3, TKey4, WeakReference> _inner;
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, WeakReference>();
			_instantiator = instantiator;
			_contains = contains;
			AddToCleanupList(this);
		}
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, WeakReference>(comparer1, comparer2, comparer3, comparer4);
			_instantiator = instantiator;
			_contains = contains;
		}
		protected override void Cleanup()
		{
			Tuple<TKey1, TKey2, TKey3, TKey4>[] keys;
			lock(_inner)
			{
				keys = ((IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4>>) _inner).ToArray();
			}
			foreach(var key in keys)
			{
				lock(_inner)
				{
					WeakReference r;
					object v;
					if(_inner.TryGetValue(key, out r) && !r.IsAlive)
					{
						_inner.Remove(key);
					}
				}
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2, key3, key4);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4]
		{
			get
			{
				WeakReference r;
				TValue v;
				lock(_inner)
				{
					if(_inner.TryGetValue(key1, key2, key3, key4, out r))
					{
						v = r.Target as TValue;
						if(v != null)
						{
							return v;
						}
					}
					_inner[key1, key2, key3, key4] = new WeakReference(v = _instantiator(key1, key2, key3, key4));
					return v;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, out TValue value)
		{
			WeakReference r;
			lock(_inner)
			{
				if(_inner.TryGetValue(key1, key2, key3, key4, out r))
				{
					value = r.Target as TValue;
					return value != null;
				}
				value = null;
				return false;
			}
		}
	}
	public interface IDomain<TKey1, TKey2, TKey3, TKey4, TKey5> : IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>>
	{
		bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5);
		int Count
		{
			get;
		}
		bool IsFinite
		{
			get;
		}
		bool IsNumerable
		{
			get;
		}
	}
	public interface IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, out TValue>
	{
		TKey1 Key1
		{
			get;
		}
		TKey2 Key2
		{
			get;
		}
		TKey3 Key3
		{
			get;
		}
		TKey4 Key4
		{
			get;
		}
		TKey5 Key5
		{
			get;
		}
		TValue Value
		{
			get;
		}
	}
	public struct KeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> : IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>
	{
		KeyValuePair<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>, TValue> inner;
		public KeyValueTuple(KeyValuePair<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>, TValue> inner)
		{
			this.inner = inner;
		}
		public TKey1 Key1
		{
			get
			{
				return inner.Key.Item1;
			}
		}
		public TKey2 Key2
		{
			get
			{
				return inner.Key.Item2;
			}
		}
		public TKey3 Key3
		{
			get
			{
				return inner.Key.Item3;
			}
		}
		public TKey4 Key4
		{
			get
			{
				return inner.Key.Item4;
			}
		}
		public TKey5 Key5
		{
			get
			{
				return inner.Key.Item5;
			}
		}
		public TValue Value
		{
			get
			{
				return inner.Value;
			}
		}
	}
	public interface IMapping<TKey1, TKey2, TKey3, TKey4, TKey5, out TValue> : IEnumerable<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>>, IDomain<TKey1, TKey2, TKey3, TKey4, TKey5>
	{
		TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5]
		{
			get;
		}
		IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>> GetEnumerator();
	}
	public class Mapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> : IMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>
	{
		public delegate TValue GetDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5);
		GetDelegate _getter;
		public Mapping(GetDelegate getter)
		{
			_getter = getter;
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5]
		{
			get
			{
				return _getter(key1, key2, key3, key4, key5);
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5)
		{
			return true;
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
	}
	public class DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> : Dictionary<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>, TValue>, IMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>
	{
		protected class EqualityComparer : IEqualityComparer<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>>
		{
			IEqualityComparer<TKey1> comparer1;
			IEqualityComparer<TKey2> comparer2;
			IEqualityComparer<TKey3> comparer3;
			IEqualityComparer<TKey4> comparer4;
			IEqualityComparer<TKey5> comparer5;
			public EqualityComparer(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4, IEqualityComparer<TKey5> comparer5)
			{
				this.comparer1 = (comparer1 == null ? EqualityComparer<TKey1>.Default : comparer1);
				this.comparer2 = (comparer2 == null ? EqualityComparer<TKey2>.Default : comparer2);
				this.comparer3 = (comparer3 == null ? EqualityComparer<TKey3>.Default : comparer3);
				this.comparer4 = (comparer4 == null ? EqualityComparer<TKey4>.Default : comparer4);
				this.comparer5 = (comparer5 == null ? EqualityComparer<TKey5>.Default : comparer5);
			}
			public bool Equals(Tuple<TKey1, TKey2, TKey3, TKey4, TKey5> a, Tuple<TKey1, TKey2, TKey3, TKey4, TKey5> b)
			{
				return comparer1.Equals(a.Item1, b.Item1) && comparer2.Equals(a.Item2, b.Item2) && comparer3.Equals(a.Item3, b.Item3) && comparer4.Equals(a.Item4, b.Item4) && comparer5.Equals(a.Item5, b.Item5);
			}
			public int GetHashCode(Tuple<TKey1, TKey2, TKey3, TKey4, TKey5> obj)
			{
				int result = 5;
				unchecked
				{
					result = result * 23 + comparer1.GetHashCode(obj.Item1);
					result = result * 23 + comparer2.GetHashCode(obj.Item2);
					result = result * 23 + comparer3.GetHashCode(obj.Item3);
					result = result * 23 + comparer4.GetHashCode(obj.Item4);
					result = result * 23 + comparer5.GetHashCode(obj.Item5);
				}
				return result;
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5)
		{
			return this.ContainsKey(new Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5));
		}
		public bool IsFinite
		{
			get
			{
				return true;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return true;
			}
		}
		public DictionaryMapping() : base()
		{
		}
		public DictionaryMapping(IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4, IEqualityComparer<TKey5> comparer5) : base (new EqualityComparer(comparer1, comparer2, comparer3, comparer4, comparer5))
		{
		}
		public new IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>> GetEnumerator()
		{
			for(var e = base.GetEnumerator(); e.MoveNext(); )
			{
				yield return new KeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(e.Current);
			}
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>>.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.Keys.GetEnumerator();
		}
		public bool ContainsKey(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5)
		{
			return base.ContainsKey(new Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5));
		}
		public bool Remove(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5)
		{
			return base.Remove(new Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5));
		}
		public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, TValue value)
		{
			base.Add(new Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5), value);
		}
		public bool TryGetValue(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, out TValue value)
		{
			return base.TryGetValue(new Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5), out value);
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5]
		{
			get
			{
				return base[new Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5)];
			}
			set
			{
				base[new Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5)] = value;
			}
		}
	}
	public class LazyMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> : IMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> _inner;
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>();
			_instantiator = instantiator;
			_contains = contains;
		}
		public LazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4, IEqualityComparer<TKey5> comparer5)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(comparer1, comparer2, comparer3, comparer4, comparer5);
			_instantiator = instantiator;
			_contains = contains;
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2, key3, key4, key5);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5]
		{
			get
			{
				TValue value;
				if(_inner.TryGetValue(key1, key2, key3, key4, key5, out value))
				{
					return value;
				}
				else
				{
					_inner[key1, key2, key3, key4, key5] = value = _instantiator(key1, key2, key3, key4, key5);
					return value;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, out TValue value)
		{
			return _inner.TryGetValue(key1, key2, key3, key4, key5, out value);
		}
	}
	public class WeakLazyMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> : WeakMapping, IMapping<TKey1, TKey2, TKey3, TKey4, TKey5, TValue> where TValue : class
	{
		public delegate TValue InstantiateDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5);
		public delegate bool ContainsDelegate(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5);
		protected InstantiateDelegate _instantiator;
		protected ContainsDelegate _contains;
		protected DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TKey5, WeakReference> _inner;
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains = null)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TKey5, WeakReference>();
			_instantiator = instantiator;
			_contains = contains;
			AddToCleanupList(this);
		}
		public WeakLazyMapping(InstantiateDelegate instantiator, ContainsDelegate contains, IEqualityComparer<TKey1> comparer1, IEqualityComparer<TKey2> comparer2, IEqualityComparer<TKey3> comparer3, IEqualityComparer<TKey4> comparer4, IEqualityComparer<TKey5> comparer5)
		{
			_inner = new DictionaryMapping<TKey1, TKey2, TKey3, TKey4, TKey5, WeakReference>(comparer1, comparer2, comparer3, comparer4, comparer5);
			_instantiator = instantiator;
			_contains = contains;
		}
		protected override void Cleanup()
		{
			Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>[] keys;
			lock(_inner)
			{
				keys = ((IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>>) _inner).ToArray();
			}
			foreach(var key in keys)
			{
				lock(_inner)
				{
					WeakReference r;
					object v;
					if(_inner.TryGetValue(key, out r) && !r.IsAlive)
					{
						_inner.Remove(key);
					}
				}
			}
		}
		public bool Contains(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5)
		{
			if(_contains == null)
			{
				return true;
			}
			else
			{
				return _contains(key1, key2, key3, key4, key5);
			}
		}
		public int Count
		{
			get
			{
				throw new Exception("Domain is not finite");
			}
		}
		public bool IsFinite
		{
			get
			{
				return false;
			}
		}
		public bool IsNumerable
		{
			get
			{
				return false;
			}
		}
		public IEnumerator<IKeyValueTuple<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>> GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>> IEnumerable<Tuple<TKey1, TKey2, TKey3, TKey4, TKey5>>.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			throw new Exception("Domain is non-numerable");
		}
		public TValue this[TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5]
		{
			get
			{
				WeakReference r;
				TValue v;
				lock(_inner)
				{
					if(_inner.TryGetValue(key1, key2, key3, key4, key5, out r))
					{
						v = r.Target as TValue;
						if(v != null)
						{
							return v;
						}
					}
					_inner[key1, key2, key3, key4, key5] = new WeakReference(v = _instantiator(key1, key2, key3, key4, key5));
					return v;
				}
			}
		}
		public bool TryGetExisting(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, out TValue value)
		{
			WeakReference r;
			lock(_inner)
			{
				if(_inner.TryGetValue(key1, key2, key3, key4, key5, out r))
				{
					value = r.Target as TValue;
					return value != null;
				}
				value = null;
				return false;
			}
		}
	}
}
