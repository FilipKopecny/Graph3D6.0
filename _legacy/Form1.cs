using graf3d;

namespace Graph3D6._0
{
	public partial class Form1 : Form
	{

		Graph3d g = new Graph3d();

		public Form1()
		{
			InitializeComponent();
			g.init();
		}

		private void bBody_Click(object sender, EventArgs e)
		{
			g.main('1', this.picBox);
		}

		private void bLines_Click(object sender, EventArgs e)
		{
			g.main('2', this.picBox);
		}

		private void bTriangles_Click(object sender, EventArgs e)
		{
			g.main('3', this.picBox);
		}

		private void bGouraud_Click(object sender, EventArgs e)
		{
			g.main('5', this.picBox);
		}

		private void bPhong_Click(object sender, EventArgs e)
		{
			g.main('4', this.picBox);
		}

		private void bPlochy_Click(object sender, EventArgs e)
		{
			g.main('3', this.picBox);
		}
	}
}