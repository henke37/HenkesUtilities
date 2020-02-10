using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Henke37.Collections.Filtered {
	public class DiffingCollection<TItem> : INotifyCollectionChanged, IReadOnlyCollection<TItem> where TItem : IEquatable<TItem> {
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private IReadOnlyCollection<TItem> _realCollection;

		private List<TItem> oldItems;

		public int Count => _realCollection.Count;

		public IEnumerator<TItem> GetEnumerator() => _realCollection.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() {
			return _realCollection.GetEnumerator();
		}

		public DiffingCollection(IReadOnlyCollection<TItem> real) {
			_realCollection = real ?? throw new ArgumentNullException(nameof(real));

			oldItems = new List<TItem>(real);

			if(_realCollection is INotifyCollectionChanged ch) {
				ch.CollectionChanged += realCollectionChanged;
			}
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

		public IReadOnlyCollection<TItem> RealCollection {
			get => _realCollection;
			set {
				if(value is null) throw new ArgumentNullException(nameof(value));

				if(_realCollection is INotifyCollectionChanged oldch) {
					oldch.CollectionChanged -= realCollectionChanged;
				}
				if(value is INotifyCollectionChanged newch) {
					newch.CollectionChanged += realCollectionChanged;
				}

				_realCollection = value;
				Diff();
			}
		}

		private void Diff() {
			var diffEntries = new List<DiffEntry>();

			//find deletes
			{
				var newItemItr = _realCollection.GetEnumerator();
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

						diffEntries.Add(new DiffEntry(EntryType.Delete, oldItemIndex, (TItem)oldItem));

						++oldItemIndex;
					}
				}

				int trailingRemovedCount = oldItems.Count - oldItemIndex - 1;

				if(trailingRemovedCount > 0) {
					for(; oldItemIndex < oldItems.Count; ++oldItemIndex) {
						diffEntries.Add(new DiffEntry(EntryType.Delete, oldItemIndex, oldItems[oldItemIndex]));
					}
				}
			}


			//find adds
			{
				var newItemItr = _realCollection.GetEnumerator();
				int oldItemIndex = 0;

				if(oldItems.Count > 0) {
					//look at each new item, checking if there is a matching old item
					while(newItemItr.MoveNext()) {
						IEquatable<TItem> oldItem = oldItems[oldItemIndex];
						IEquatable<TItem> newItem = newItemItr.Current;

						if(oldItem == newItem) {
							if(oldItemIndex+1 < oldItems.Count) {
								++oldItemIndex;
								continue;
							} else {
								break;
							}

						}

						diffEntries.Add(new DiffEntry(EntryType.Add, oldItemIndex, (TItem)newItem));
					}
				}

				while(newItemItr.MoveNext()) {
					TItem newItem = newItemItr.Current;

					diffEntries.Add(new DiffEntry(EntryType.Add, oldItemIndex, newItem));
				}
			}

			diffEntries.Sort();

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
							int startingIndex = firstDeletedItem.Index + indexOffset;
							oldItems.RemoveRange(startingIndex, deltaItems.Count);
							DispatchEvent(NotifyCollectionChangedAction.Remove, removedItems, startingIndex);
							//indexOffset -= deltaItems.Count-1;
							break;
						}

						case EntryType.Add: {
							DiffEntry firstAddedItem = deltaItems[0];
							IList addedItems = ElementsInEntries(deltaItems);
							int startingIndex = firstAddedItem.Index + indexOffset;
							oldItems.InsertRange(startingIndex, (IList<TItem>)addedItems);
							DispatchEvent(NotifyCollectionChangedAction.Add, addedItems, startingIndex);
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
			public readonly EntryType EntryType;
			public readonly int Index;
			public readonly TItem Item;

			public DiffEntry(EntryType type, int itemIndex, TItem item) {
				EntryType = type;
				Index = itemIndex;
				Item = item;
			}

			public int CompareTo(DiffEntry other) {
				int res= Index.CompareTo(other.Index);
				if(res != 0) return res;
				return EntryType.CompareTo(other.EntryType);
			}

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

			public override string ToString() {
				return $"{EntryType} {Index} {Item}";
			}
		}

		private enum EntryType {
			Invalid,
			Add,
			Delete
		}
	}
}
