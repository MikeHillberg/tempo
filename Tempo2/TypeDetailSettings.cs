using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;

namespace Tempo
{
    public class HomePageSettings : INotifyPropertyChanged
    {
        public HomePageSettings()
        {
            Settings.Changed += (s, e) =>
             {
                 UpdateFilterBackground();
             };
        }

        bool _isWide = true;
        public bool IsWide
        {
            get { return _isWide; }
            set
            {
                _isWide = value;
                _isSkinny = !value;
                RaisePropertyChanged();
            }
        }

        bool _isSkinny = false;
        public bool IsSkinny
        {
            get { return _isSkinny; }
            set
            {
                _isSkinny = value;
                _isWide = !value;
                RaisePropertyChanged();
            }
        }

        SolidColorBrush _filterBackground = new SolidColorBrush(Colors.Transparent);
        public Brush FilterBackground
        {
            get { return _filterBackground; }
        }
        public void UpdateFilterBackground()
        {
            // If any filters are set, highlight the filter button's background so you have
            // a clue about it

            if (Manager.Settings.IsDefault)
            {
                if (_filterBackground.Color != Colors.Transparent)
                {
                    _filterBackground = new SolidColorBrush(Colors.Transparent);
                    RaisePropertyChanged(nameof(FilterBackground));
                }
            }
            else
            {
                if (_filterBackground.Color == Colors.Transparent)
                {
                    // aka SystemAccentColorLight2
                    var color = (new UISettings()).GetColorValue(UIColorType.AccentLight2);

                    _filterBackground = new SolidColorBrush(color);
                    RaisePropertyChanged(nameof(FilterBackground));
                }
            }
        }

        void RaisePropertyChanged(string name = "" )
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
