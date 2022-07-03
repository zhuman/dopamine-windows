﻿using Prism.Ioc;
using Dopamine.Core.Prism;
using Prism.Events;
using System.Windows.Controls;

namespace Dopamine.Views.Common
{
    public partial class SearchControl : UserControl
    {
        public SearchControl()
        {
            InitializeComponent();

            IEventAggregator eventAggregator = ContainerLocator.Current.Resolve<IEventAggregator>();

            eventAggregator.GetEvent<FocusSearchBox>().Subscribe((_) => this.SearchBox.SetKeyboardFocus());
        }
    }
}
