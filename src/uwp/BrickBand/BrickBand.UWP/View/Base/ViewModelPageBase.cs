using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Autofac;
using BrickBand.UWP.Annotations;
using BrickBand.UWP.Model.Contract;

namespace BrickBand.UWP.View.Base
{
    public class ViewModelPageBase : Page,INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected ISettingsService SettingsService;



        public ViewModelPageBase()
        {
            SettingsService = App.Glue.Container.Resolve<ISettingsService>();//I hate service location, but this app is meant to be simple so we're not going for injectable view models etc. At least it's not service location once in model land :/
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
