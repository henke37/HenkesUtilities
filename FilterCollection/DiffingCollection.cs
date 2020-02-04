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

			//dispatch events
			{
				var deltaItems = new List<DiffEntry>();
				EntryType runType = EntryType.Invalid;
				int indexOffset = 0;

				void DispatchPendingEntries() {
					if(deltaItems.Count == 0) return;

					switch(runType) {
						case EntryType.Invalid:
							throw new Exception();
						case EntryType.Delete: {
							DiffEntry firstDeletedItem = deltaItems[0];
							IList removedItems = ElementsInEntries(deltaItems);
							DispatchEvent(NotifyCollectionChangedAction.Remove, removedItems, firstDeletedItem.Index + indexOffset);
							indexOffset -= deltaItems.Count;
							break;
						}

						case EntryType.Add: {
							DiffEntry firstAddedItem = deltaItems[0];
							IList addedItems = ElementsInEntries(deltaItems);
							DispatchEvent(NotifyCollectionChangedAction.Add, addedItems, firstAddedItem.Index + indexOffset);
							indexOffset += deltaItems.Count;
							break;
						}
					}
				}

				
				foreach(var entry in diffEntries) {

					if(entry.EntryType==runType) {
						deltaItems.Add(entry);
						continue;
					}

					DispatchPendingEntries();

					runType = entry.EntryType;
					deltaItems.Clear();
					deltaItems.Add(entry);					
				}

				DispatchPendingEntries();
			}
		}

		private static IList ElementsInEntries(List<DiffEntry> deltaItems) {
			var l=new List<TItem>();
			foreach(var entry in deltaItems) {
				l.Add(entry.Item);
			}
			return l;
		}

		private void DispatchEvent(NotifyCollectionChangedAction action, IList changedItems, int startingIndex) {
			CollectionChanged.Invoke(this, new NotifyCollectionChangedEventArgs(action,changedItems,startingIndex));
		}

		private struct DiffEntry : IComparable<DiffEntry>, IEquatable<DiffEntry> {
			public EntryType EntryType;
			public int Index;
			public TItem Item;

			public DiffEntry(EntryType type, int itemIndex, TItem item) {
				EntryType = type;
				Index = itemIndex;
				Item = item;
			}

			public int CompareTo(DiffEntry other) { return Index.CompareTo(other.Index); }

			public bool Equals(DiffEntry other) {
				if(EntryType != other.EntryType) return false;
				if(Index != other.Index) return false;
				if((IEquatable <TItem>)Item != (IEquatable<TItem>)other.Item) return false;
				return true;
			}

			public override bool Equals(object other) {
				if(other is DiffEntry o2) return Equals(o2);
				return false;
			}

			public static bool operator ==(DiffEntry left, DiffEntry right) {
				if(ReferenceEquals(left, null)) {
					return ReferenceEquals(right, null);
				}
				return left.Equals(right);
			}
			public static bool operator >(DiffEntry left, DiffEntry right) {
				return left.CompareTo(right) > 0;
			}
			public static bool operator <(DiffEntry left, DiffEntry right) {
				return left.CompareTo(right) < 0;
			}
			public static bool operator !=(DiffEntry left, DiffEntry right) {
				return !(left == right);
			}

			public override int GetHashCode() {
				return Index.GetHashCode() ^ EntryType.GetHashCode() ^ Item.GetHashCode();
			}
		}

		private enum EntryType {
			Invalid,
			Add,
			Delete
		}
	}
}
