using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reynolds.Mappings
{
	class ReferenceTypeArrayEqualityComparer<T> : IEqualityComparer<T[]>, IComparer<T[]> where T : class, IComparable<T>
	{
		protected ReferenceTypeArrayEqualityComparer()
		{
		}

		public readonly static ReferenceTypeArrayEqualityComparer<T> Instance = new ReferenceTypeArrayEqualityComparer<T>();

		public bool Equals(T[] x, T[] y)
		{
			if(x.Length != y.Length)
				return false;
			for(int k = 0; k < x.Length; k++)
				if(x[k] != y[k])
					return false;
			return true;
		}

		public int GetHashCode(T[] array)
		{
			int result = array.Length;
			for(int i = 0; i < array.Length; i++)
			{
				unchecked
				{
					result = result * 23 + array[i].GetHashCode();
				}
			}
			return result;
		}

		public int Compare(T[] x, T[] y)
		{
			if(x == y)
				return 0;
			int c;
			if(0 != (c = x.Length.CompareTo(y.Length)))
				return c;
			for(int k = 0; k < x.Length; k++)
				if(0 != (c = x[k].CompareTo(y[k])))
					return c;
			return 0;
		}
	}

	class ValueTypeArrayEqualityComparer<T> : IEqualityComparer<T[]>, IComparer<T[]> where T : struct, IComparable<T>
	{
		protected ValueTypeArrayEqualityComparer()
		{
		}

		public readonly static ValueTypeArrayEqualityComparer<T> Instance = new ValueTypeArrayEqualityComparer<T>();

		public bool Equals(T[] x, T[] y)
		{
			if(x.Length != y.Length)
				return false;
			for(int k = 0; k < x.Length; k++)
				if(x[k].CompareTo(y[k]) != 0)
					return false;
			return true;
		}

		public int GetHashCode(T[] array)
		{
			int result = array.Length;
			for(int i = 0; i < array.Length; i++)
			{
				unchecked
				{
					result = result * 23 + array[i].GetHashCode();
				}
			}
			return result;
		}

		public int Compare(T[] x, T[] y)
		{
			if(x == y)
				return 0;
			int c;
			if(0 != (c = x.Length.CompareTo(y.Length)))
				return c;
			for(int k = 0; k < x.Length; k++)
				if(0 != (c = x[k].CompareTo(y[k])))
					return c;
			return 0;
		}
	}
}
