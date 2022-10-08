using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonLibrary
{
    public class MyAsyncOperation
    {
        EventHandler _completed = null;
        public event EventHandler Completed
        {
            add
            {
                _completed += value;

                if (_isComplete)
                    Complete();
            }

            remove
            {
                _completed -= null;
            }
        }


        bool _isComplete = false;
        public void Complete()
        {
            _isComplete = true;

            if (_completed != null)
                _completed(this, null);
        }
    }

}
