using System;

namespace Tempo
{
    public class TypeNavigatedEventArgs : EventArgs
    {
        private int _index;
        public TypeNavigatedEventArgs(int index)
        {
            _index = index;
        }

        public TypeNavigatedEventArgs(int index, MemberOrTypeViewModelBase memberViewModel) : this(index)
        {
            MemberViewModel = memberViewModel;
        }

        public int Index {  get { return _index; } }

        public MemberOrTypeViewModelBase MemberViewModel { get; }
    }
}