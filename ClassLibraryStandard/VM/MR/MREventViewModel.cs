using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class MREventViewModel : EventViewModel
    {
        MRTypeViewModel _declaringMrType;
        MrEvent _ev;
        public MREventViewModel(MRTypeViewModel declaringType, MrEvent ev)
        {
            _declaringMrType = declaringType;
            _ev = ev;
        }

        public override TypeViewModel ReturnType => this.EventHandlerType;

        string _name;
        public override string Name
        {
            get
            {
                if(_name == null)
                {
                    _name = _ev.GetName();
                }
                return _name;
            }
        }

        public override MyMemberTypes MemberType => MyMemberTypes.Event;


        MRMethodViewModel _adder;
        MRMethodViewModel _remover;
        protected override MethodViewModel CreateAdderMethodViewModel()
        {
            if(_adder == null)
            {
                _ev.GetAccessors(out var adder, out var remover);

                _adder = new MRMethodViewModel(_declaringMrType, adder);
                _remover = new MRMethodViewModel(_declaringMrType, remover);
            }

            return _adder;
        }

        protected override MethodViewModel CreateRemoverMethodViewModel()
        {
            CreateAdderMethodViewModel();
            return _remover;
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            return
                (from mrAttribute in _ev.GetCustomAttributes()
                 select new MRCustomAttributeViewModel(mrAttribute, this.DeclaringType as MRTypeViewModel))
                 .ToList<CustomAttributeViewModel>();
        }

        MRTypeViewModel _eventHandlerType;
        protected override TypeViewModel CreateEventHandlerType()
        {
            if(_eventHandlerType == null)
            {
                _eventHandlerType = MRTypeViewModel.GetFromCache(_ev.GetEventType(), _declaringMrType.TypeSet) as MRTypeViewModel;
            }

            return _eventHandlerType;
        }

        protected override MethodViewModel CreateInvoker()
        {
            var invoker = _ev.GetInvoker();
            if (invoker == null)
            {
                return null;
            }

            return new MRMethodViewModel(DeclaringType as MRTypeViewModel, _ev.GetInvoker());
        }

        public override string ToString()
        {
            return $"{this.DeclaringType.PrettyName}.{this.PrettyName}";
        }

        protected override TypeViewModel GetDeclaringType()
        {
            return _declaringMrType;
        }

        public override bool TryGetVMProperty(string key, out object value)
        {
            return MRTypeViewModel.TryGetVMPropertyHelper(this, key, out value);
        }

        public override string ToWhereString()
        {
            return PrettyName;
        }

        protected override TypeViewModel GetTypeFromCache(TypeViewModel typeViewModel)
        {
            return MRTypeViewModel.GetFromCache((typeViewModel as MRTypeViewModel).Type, _declaringMrType.TypeSet);
        }

        ParsedMethodAttributes? _memberModifiers;
        public override bool IsProtected
        {
            get
            {
                EnsureMemberModifiers();

                return _memberModifiers.Value.IsProtected;
            }
        }

        private void EnsureMemberModifiers()
        {
            if (_memberModifiers == null)
            {
                _memberModifiers = _ev.GetMemberModifiers();
            }
        }


        public override bool IsInternal
        {
            get
            {
                EnsureMemberModifiers();
                return _memberModifiers.Value.IsInternal;
            }
        }
    }
}
