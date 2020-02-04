using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Henke37.Collections.Filtered {
	public class DiffingCollection<TItem> : INotifyCollectionChanged, IReadOnlyCollection<TItem> where TItem : IEquatable<TItem> {
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private IReadOnlyCollection<TItem> realCollection;

		private List<TItem> oldItems;

		public int Count => realCollection.Count;

		public IEnumerator<TItem> GetEnumerator() => realCollection.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() {
			return realCollection.GetEnumerator();
		}

		public DiffingCollection(IReadOnlyCollection<TItem> real) {
			realCollection = real ?? throw new ArgumentNullException(nameof(real));

			oldItems = new List<TItem>(real);

			var ch = (INotifyCollectionChanged)realCollection;
			ch.CollectionChanged += realCollectionChanged;
		}

		private void realCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			switch(e.Action) {
				case NotifyCollectionChangedAction.Reset:
					Diff();
					return;

				case NotifyCollectionChangedAction.Add:
					break;
				case NotifyCollectionChangedAction.Remove:
					break;
				case NotifyCollectionChangedAction.Replace:
					//oldItems[e.OldStartingIndex] = (TItem)e.NewItems[0];
					break;
			}
			throw new NotImplementedException();
		}

		private void Diff() {
			var diffEntries = new List<DiffEntry>();

			//find deletes
			{
				var newItemItr = realCollection.GetEnumerator();
				int oldItemIndex = 0;


				if(newItemItr.MoveNext()) {

					for(; oldItemIndex < oldItems.Count; ) {
						IEquatable<TItem> oldItem = oldItems[oldItemIndex];
						IEquatable<TItem> newItem = newItemItr.Current;
						if(oldItem == newItem) {

							++oldItemIndex;
							if(newItemItr.MoveNext()) {
								continue;
							} else {
								break;
							}
						}

						diffEntries.Add(new DiffEntry(EntryType.Add, oldItemIndex, (TItem)oldItem));
					}
				}

				int trailingRemovedCount = oldItems.Count - oldItemIndex - 1;

				if(trailingRemovedCount > 0) {
					for(; oldItemIndex < oldItems.Count; ++oldItemIndex) {
						diffEntries.Add(new DiffEntry(EntryType.Add, oldItemIndex, oldItems[oldItemIndex]));
					}
				}
			}


			//find adds
		}

		private void DispatchEvent(NotifyCollectionChangedAction action, IList changedItems, int startingIndex) {
			CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(action,changedItems,startingIndex));
		}

		private struct DiffEntry {
			public EntryType EntryType;
			public int Index;
			public TItem Item;

			public DiffEntry(EntryType type, int itemIndex, TItem item) {
				EntryType = type;
				Index = itemIndex;
				Item = item;
			}
		}

		private enum EntryType {
			Add,
			Delete
		}
	}
}
