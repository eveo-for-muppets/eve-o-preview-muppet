using System;
using System.Windows;

namespace EveOMock
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			Random random = new Random();
			this.Title += random.Next().ToString("X");
		}
	}
}
