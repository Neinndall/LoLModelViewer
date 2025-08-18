using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media.Media3D;

namespace LoLModelViewer.Models
{
    public class BindableModelVisual3D : ModelVisual3D
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<ModelVisual3D>),
            typeof(BindableModelVisual3D), new PropertyMetadata(null, OnItemsSourceChanged));

        public ObservableCollection<ModelVisual3D> ItemsSource
        {
            get { return (ObservableCollection<ModelVisual3D>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var source = (BindableModelVisual3D)d;
            if (e.OldValue != null)
            {
                var coll = (INotifyCollectionChanged)e.OldValue;
                coll.CollectionChanged -= source.OnCollectionChanged;
            }
            if (e.NewValue != null)
            {
                var coll = (INotifyCollectionChanged)e.NewValue;
                source.Children.Clear();
                foreach (var item in (ObservableCollection<ModelVisual3D>)e.NewValue)
                {
                    source.Children.Add(item);
                }
                coll.CollectionChanged += source.OnCollectionChanged;
            }
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        Children.Add((ModelVisual3D)item);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        Children.Remove((ModelVisual3D)item);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                Children.Clear();
            }
        }
    }
}
