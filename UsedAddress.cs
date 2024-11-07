using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Jamarino.IntervalTree;

namespace AbakConfigurator.IEC
{
    public struct Address
    {
        public int address;
        public int count;
        public WeakReference owner;



        public bool Intersect(object owner, int address, int count)
        {
            if (this.owner.Target.Equals(owner))
            {
                return false;
            }

            var range = Enumerable.Range(address, count);
            var this_range = Enumerable.Range(this.address, this.count);

            return this_range.Intersect(range).Count() != 0;
        }
    }

    public class UsedAddress : IObservable<UsedAddress>
    {
        const int maxAddress = 65536;
        List<IObserver<UsedAddress>> observers = new List<IObserver<UsedAddress>>();
        QuickIntervalTree<int, Address> intervals = new QuickIntervalTree<int, Address>();

        SortedSet<int> free_address = new SortedSet<int>();

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<UsedAddress>> _observers;
            private IObserver<UsedAddress> _observer;

            public Unsubscriber(List<IObserver<UsedAddress>> observers, IObserver<UsedAddress> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (!(_observer == null)) _observers.Remove(_observer);
            }
        }
        private void Notify(IEnumerable<Address> intersected)
        {
            foreach (var i in intersected)
            {
                var o = i.owner.Target as IObserver<UsedAddress>;
                o.OnNext(this);
            }
        }

        public UsedAddress(int min_address = 0)
        {
            for (int i = min_address; i < ushort.MaxValue; i++)
            {
                free_address.Add(i);
            }
        }
        public UsedAddress(UsedAddress other)
        {
            free_address = new SortedSet<int>(other.free_address);
        }

        public IDisposable Subscribe(IObserver<UsedAddress> o)
        {
            if (!observers.Contains(o))
                observers.Add(o);

            return new Unsubscriber(observers, o);
        }

        public void Add(object owner, int address, int count)
        {
            var a = new Address
            {
                address = address,
                count = count,
                owner = new WeakReference(owner)
            };

            intervals.Add(address, address + count - 1, a);

            for (int i = 0; i < count; i++)
            {
                free_address.Remove(address + i);
            }

            var intersected = intervals.Query(address, address + count - 1);
            Notify(intersected);
        }

        public void Remove(object owner)
        {
            var a = intervals.Values.First(i => i.owner.Target.Equals(owner));
            intervals.Remove(a);
            for (int i = 0; i < a.count; i++)
            {
                free_address.Add(a.address + i);
            }

            var intersected = intervals.Query(a.address, a.address + a.count - 1);
            Notify(intersected);
        }

        public void Replace(object owner, int address, int count)
        {
            Remove(owner);
            Add(owner, address, count);
        }

        public bool Intersect(object owner, int address, int count)
        {
            var intersected = intervals.Query(address, address + count - 1);
            return intersected.Count() > 1;
        }

        public int GetFreeAddress(int startAddress, int count)
        {
            return free_address.First();
        }
    }
}
