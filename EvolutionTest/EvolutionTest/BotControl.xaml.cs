using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EvolutionTest
{
	/// <summary>
	/// Interaction logic for BotControl.xaml
	/// </summary>
	public partial class BotControl : UserControl
	{
		public Entity Obj { get; set; }
		public Color PrevColor { get; set; }

		public BotControl(Entity bot)
			: base()
		{
			Obj = bot;
			InitializeComponent();
		}
	}
}
