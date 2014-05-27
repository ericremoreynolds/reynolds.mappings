using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Reynolds.Mappings
{
	public abstract class WeakMapping
	{
		protected abstract void Cleanup();

		static Thread cleanupThread;

		static List<WeakReference> containers = null;
		protected static List<WeakReference> Containers
		{
			get
			{
				if(containers == null)
					containers = new List<WeakReference>();
				return containers;
			}
		}

		protected static void AddToCleanupList(WeakMapping container)
		{
			lock(Containers)
			{
				Containers.Add(new WeakReference(container));
				if(cleanupThread == null)
				{
					cleanupThread = new Thread(CleanupLoop);
					cleanupThread.Priority = ThreadPriority.BelowNormal;
					cleanupThread.Start();
				}
			}
		}

		static void CleanupLoop()
		{
			WeakMapping container;
			while(true)
			{
				Thread.Sleep(30);
				int k = 0;
				while(k < containers.Count)
				{
					lock(containers)
					{
						container = containers[k].Target as WeakMapping;
						if(container == null)
							containers.RemoveAt(k);
						else
						{
							container.Cleanup();
							k++;
						}
					}
				}
			}
		}
	}
}
