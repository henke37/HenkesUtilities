using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Henke37.Collections.Filtered {
	public class FilterCollection<TItem> : INotifyCollectionChanged, IReadOnlyCollection<TItem> {
		private IReadOnlyCollection<TItem> realCollection;

		public event NotifyCollectionChangedEventHandler CollectionChanged;
		private Func<TItem, bool> filterFunc;

		public FilterCollection(IReadOnlyCollection<TItem> realCollection, Func<TItem, bool> filterFunc) {
			this.realCollection = realCollection ?? throw new ArgumentNullException(nameof(realCollection));
			this.filterFunc = filterFunc ?? throw new ArgumentNullException(nameof(filterFunc));
		}

		public Func<TItem, bool> FilterFunc {
			get => filterFunc;
			set {
				if(value == filterFunc) return;
				filterFunc = value;
				ListChanged();
			}
		}

		public IReadOnlyCollection<TItem> RealCollection {
			get => realCollection;
			set {
				realCollection = value;
				ListChanged();
			}
		}

		private void ListChanged() {
			CollectionChanged?.Invoke(this,new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		public int Count => realCollection.Count(filterFunc);

		public IEnumerator<TItem> GetEnumerator() {
			foreach(var item in realCollection) {
				if(!filterFunc(item)) continue;
				yield return item;
			}
			yield break;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
