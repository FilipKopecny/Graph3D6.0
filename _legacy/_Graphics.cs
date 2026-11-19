using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace graf3d
{

	public class DirectBitmap : IDisposable
	{
		public Bitmap Bitmap { get; private set; }
		public Int32[] Bits { get; private set; }
		public bool Disposed { get; private set; }
		public int Height { get; private set; }
		public int Width { get; private set; }

		protected GCHandle BitsHandle { get; private set; }

		public DirectBitmap(int width, int height)
		{
			Width = width;
			Height = height;
			Bits = new Int32[width * height];
			BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
			Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
		}

		public void SetPixel(int x, int y, Color colour)
		{
			int index = (x) + ((y) * Width);
			int col = colour.ToArgb();
			if (index > 0)
				Bits[index] = col;
		}

		public Color GetPixel(int x, int y)
		{
			int index = x + (y * Width);
			int col = Bits[index];
			Color result = Color.FromArgb(col);

			return result;
		}

		public void Dispose()
		{
			if (Disposed)
				return;
			Disposed = true;
			Bitmap.Dispose();
			BitsHandle.Free();
		}
	}

	internal class Graph3d
	{
		Pen pen = new Pen(Color.White);

		int x, y;
		char ch;
		int a;

		string prvok;

		//ifstream vstup;

		int mbs, mbe, k, t, xo1, yo1, xo2, yo2, p, ii, dy, dx;

		double x1, y1, z1, di;

		private struct Bod
		{
			public double x, y, z, norx, nory, norz;
		}; // sem by sa este zislo dat smerniky na plochy, ale uz mam
		   // spravenu tu pomalu proceduru(voidfciu), takze...

		private struct Kvader
		{
			public double x, y, z, dx, dy, dz, xuhol, yuhol, zuhol;
		};

		private struct Meshbs
		{
			public Meshbs()
			{ }
			public double x = 0, y = 0, z = 0, xuhol = 0, yuhol = 0, zuhol = 0;
			public Bod[,] b = new Bod[5, 5];
		};

		private struct Meshbe
		{
			public Meshbe()
			{ }
			public double x = 0, y = 0, z = 0, xuhol = 0, yuhol = 0, zuhol = 0;
			public Bod[,] b = new Bod[7, 7];
		};

		private struct Toroid
		{
			public double x, y, z, rin, rout, xuhol, yuhol, zuhol;
			public int xjemnost, yjemnost;
		};

		private struct Kamera
		{
			public double xk, yk, zk, uhol;
		};

		private struct Hrana
		{
			public int bod1, bod2;
		};

		private struct Plocha
		{
			public int bod1, bod2, bod3;
			public double norx, nory, norz;
			public int zaobli;
		};

		int PocetUspPrivrPloch;

		private Bod[] body = new Bod[10000];
		private Hrana[] hrany = new Hrana[10000];
		private Plocha[] plochy = new Plocha[10000];

		private int[] uspplochy = new int[1300];


		private Kvader[] kvadre = new Kvader[5];
		private Toroid[] toroidy = new Toroid[5];
		private Meshbs[] meshebs = new Meshbs[5];
		private Meshbe[] meshebe = new Meshbe[5];
		private Kamera kamera;

		private int pocetbodov = 0;
		private int pocethran = 0;
		private int pocetploch = 0;
		private int yres = 800, xres = 1000;

		private DirectBitmap db = new DirectBitmap(1000, 800);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="p"></param>
		private void normalizuj(int p)
		{
			double[] v1 = new double[3];
			double[] v2 = new double[3];

			double min;

			v1[0] = body[plochy[p].bod2].x - body[plochy[p].bod1].x;
			v1[1] = body[plochy[p].bod2].y - body[plochy[p].bod1].y;
			v1[2] = body[plochy[p].bod2].z - body[plochy[p].bod1].z;

			v2[0] = body[plochy[p].bod3].x - body[plochy[p].bod1].x;
			v2[1] = body[plochy[p].bod3].y - body[plochy[p].bod1].y;
			v2[2] = body[plochy[p].bod3].z - body[plochy[p].bod1].z;

			plochy[p].norx = v1[1] * v2[2] - v2[1] * v1[2];     // normala
			plochy[p].nory = v1[2] * v2[0] - v2[2] * v1[0];
			plochy[p].norz = v1[0] * v2[1] - v2[0] * v1[1];

			min = Math.Sqrt(plochy[p].norx * plochy[p].norx + plochy[p].nory * plochy[p].nory + plochy[p].norz * plochy[p].norz);

			if (min != 0)
			{
				plochy[p].norx /= min;
				plochy[p].nory /= min;
				plochy[p].norz /= min;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c1u"></param>
		/// <param name="c2u"></param>
		/// <param name="c3u"></param>
		/// <param name="c4u"></param>
		/// <param name="meshe"></param>
		/// <param name="c1v"></param>
		/// <param name="c2v"></param>
		/// <param name="c3v"></param>
		/// <param name="c4v"></param>
		/// <param name="xxx"></param>
		/// <param name="yyy"></param>
		/// <returns></returns>
		private double Vynasobzbs(double c1u, double c2u, double c3u, double c4u, Meshbs meshe, double c1v, double c2v, double c3v, double c4v, int xxx, int yyy)
		{
			double[] temp = new double[4];
			double tmp;

			temp[0] = c1u * meshe.b[xxx, yyy].z + c2u * meshe.b[xxx, yyy + 1].z + c3u * meshe.b[xxx, yyy + 2].z + c4u * meshe.b[xxx, yyy + 3].z;
			temp[1] = c1u * meshe.b[xxx + 1, yyy].z + c2u * meshe.b[xxx + 1, yyy + 1].z + c3u * meshe.b[xxx + 1, yyy + 2].z + c4u * meshe.b[xxx + 1, yyy + 3].z;
			temp[2] = c1u * meshe.b[xxx + 2, yyy].z + c2u * meshe.b[xxx + 2, yyy + 1].z + c3u * meshe.b[xxx + 2, yyy + 2].z + c4u * meshe.b[xxx + 2, yyy + 3].z;
			temp[3] = c1u * meshe.b[xxx + 3, yyy].z + c2u * meshe.b[xxx + 3, yyy + 1].z + c3u * meshe.b[xxx + 3, yyy + 2].z + c4u * meshe.b[xxx + 3, yyy + 3].z;

			tmp = (c1v * temp[0] + c2v * temp[1] + c3v * temp[2] + c4v * temp[3]);

			return tmp;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c1u"></param>
		/// <param name="c2u"></param>
		/// <param name="c3u"></param>
		/// <param name="c4u"></param>
		/// <param name="meshe"></param>
		/// <param name="c1v"></param>
		/// <param name="c2v"></param>
		/// <param name="c3v"></param>
		/// <param name="c4v"></param>
		/// <param name="xxx"></param>
		/// <param name="yyy"></param>
		/// <returns></returns>
		private double Vynasobybs(double c1u, double c2u, double c3u, double c4u, Meshbs meshe, double c1v, double c2v, double c3v, double c4v, int xxx, int yyy)
		{
			double[] temp = new double[4];
			double tmp;

			temp[0] = c1u * meshe.b[xxx, yyy].y + c2u * meshe.b[xxx, yyy + 1].y + c3u * meshe.b[xxx, yyy + 2].y + c4u * meshe.b[xxx, yyy + 3].y;
			temp[1] = c1u * meshe.b[xxx + 1, yyy].y + c2u * meshe.b[xxx + 1, yyy + 1].y + c3u * meshe.b[xxx + 1, yyy + 2].y + c4u * meshe.b[xxx + 1, yyy + 3].y;
			temp[2] = c1u * meshe.b[xxx + 2, yyy].y + c2u * meshe.b[xxx + 2, yyy + 1].y + c3u * meshe.b[xxx + 2, yyy + 2].y + c4u * meshe.b[xxx + 2, yyy + 3].y;
			temp[3] = c1u * meshe.b[xxx + 3, yyy].y + c2u * meshe.b[xxx + 3, yyy + 1].y + c3u * meshe.b[xxx + 3, yyy + 2].y + c4u * meshe.b[xxx + 3, yyy + 3].y;

			tmp = (c1v * temp[0] + c2v * temp[1] + c3v * temp[2] + c4v * temp[3]);

			return tmp;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c1u"></param>
		/// <param name="c2u"></param>
		/// <param name="c3u"></param>
		/// <param name="c4u"></param>
		/// <param name="meshe"></param>
		/// <param name="c1v"></param>
		/// <param name="c2v"></param>
		/// <param name="c3v"></param>
		/// <param name="c4v"></param>
		/// <param name="xxx"></param>
		/// <param name="yyy"></param>
		/// <returns></returns>
		private double Vynasobxbs(double c1u, double c2u, double c3u, double c4u, Meshbs meshe, double c1v, double c2v, double c3v, double c4v, int xxx, int yyy)
		{
			double[] temp = new double[4];
			double tmp;

			temp[0] = c1u * meshe.b[xxx, yyy].x + c2u * meshe.b[xxx, yyy + 1].x + c3u * meshe.b[xxx, yyy + 2].x + c4u * meshe.b[xxx, yyy + 3].x;
			temp[1] = c1u * meshe.b[xxx + 1, yyy].x + c2u * meshe.b[xxx + 1, yyy + 1].x + c3u * meshe.b[xxx + 1, yyy + 2].x + c4u * meshe.b[xxx + 1, yyy + 3].x;
			temp[2] = c1u * meshe.b[xxx + 2, yyy].x + c2u * meshe.b[xxx + 2, yyy + 1].x + c3u * meshe.b[xxx + 2, yyy + 2].x + c4u * meshe.b[xxx + 2, yyy + 3].x;
			temp[3] = c1u * meshe.b[xxx + 3, yyy].x + c2u * meshe.b[xxx + 3, yyy + 1].x + c3u * meshe.b[xxx + 3, yyy + 2].x + c4u * meshe.b[xxx + 3, yyy + 3].x;

			tmp = (c1v * temp[0] + c2v * temp[1] + c3v * temp[2] + c4v * temp[3]);

			return tmp;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c1u"></param>
		/// <param name="c2u"></param>
		/// <param name="c3u"></param>
		/// <param name="c4u"></param>
		/// <param name="meshe"></param>
		/// <param name="c1v"></param>
		/// <param name="c2v"></param>
		/// <param name="c3v"></param>
		/// <param name="c4v"></param>
		/// <param name="xxx"></param>
		/// <param name="yyy"></param>
		/// <returns></returns>
		private double Vynasobzbe(double c1u, double c2u, double c3u, double c4u, Meshbe meshe, double c1v, double c2v, double c3v, double c4v, int xxx, int yyy)
		{
			double[] temp = new double[4];
			double tmp;

			temp[0] = c1u * meshe.b[xxx, yyy].z + c2u * meshe.b[xxx, yyy + 1].z + c3u * meshe.b[xxx, yyy + 2].z + c4u * meshe.b[xxx, yyy + 3].z;
			temp[1] = c1u * meshe.b[xxx + 1, yyy].z + c2u * meshe.b[xxx + 1, yyy + 1].z + c3u * meshe.b[xxx + 1, yyy + 2].z + c4u * meshe.b[xxx + 1, yyy + 3].z;
			temp[2] = c1u * meshe.b[xxx + 2, yyy].z + c2u * meshe.b[xxx + 2, yyy + 1].z + c3u * meshe.b[xxx + 2, yyy + 2].z + c4u * meshe.b[xxx + 2, yyy + 3].z;
			temp[3] = c1u * meshe.b[xxx + 3, yyy].z + c2u * meshe.b[xxx + 3, yyy + 1].z + c3u * meshe.b[xxx + 3, yyy + 2].z + c4u * meshe.b[xxx + 3, yyy + 3].z;

			tmp = (c1v * temp[0] + c2v * temp[1] + c3v * temp[2] + c4v * temp[3]);

			return tmp;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c1u"></param>
		/// <param name="c2u"></param>
		/// <param name="c3u"></param>
		/// <param name="c4u"></param>
		/// <param name="meshe"></param>
		/// <param name="c1v"></param>
		/// <param name="c2v"></param>
		/// <param name="c3v"></param>
		/// <param name="c4v"></param>
		/// <param name="xxx"></param>
		/// <param name="yyy"></param>
		/// <returns></returns>
		private double Vynasobybe(double c1u, double c2u, double c3u, double c4u, Meshbe meshe, double c1v, double c2v, double c3v, double c4v, int xxx, int yyy)
		{
			double[] temp = new double[4];
			double tmp;

			temp[0] = c1u * meshe.b[xxx, yyy].y + c2u * meshe.b[xxx, yyy + 1].y + c3u * meshe.b[xxx, yyy + 2].y + c4u * meshe.b[xxx, yyy + 3].y;
			temp[1] = c1u * meshe.b[xxx + 1, yyy].y + c2u * meshe.b[xxx + 1, yyy + 1].y + c3u * meshe.b[xxx + 1, yyy + 2].y + c4u * meshe.b[xxx + 1, yyy + 3].y;
			temp[2] = c1u * meshe.b[xxx + 2, yyy].y + c2u * meshe.b[xxx + 2, yyy + 1].y + c3u * meshe.b[xxx + 2, yyy + 2].y + c4u * meshe.b[xxx + 2, yyy + 3].y;
			temp[3] = c1u * meshe.b[xxx + 3, yyy].y + c2u * meshe.b[xxx + 3, yyy + 1].y + c3u * meshe.b[xxx + 3, yyy + 2].y + c4u * meshe.b[xxx + 3, yyy + 3].y;

			tmp = (c1v * temp[0] + c2v * temp[1] + c3v * temp[2] + c4v * temp[3]);

			return tmp;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="c1u"></param>
		/// <param name="c2u"></param>
		/// <param name="c3u"></param>
		/// <param name="c4u"></param>
		/// <param name="meshe"></param>
		/// <param name="c1v"></param>
		/// <param name="c2v"></param>
		/// <param name="c3v"></param>
		/// <param name="c4v"></param>
		/// <param name="xxx"></param>
		/// <param name="yyy"></param>
		/// <returns></returns>
		private double Vynasobxbe(double c1u, double c2u, double c3u, double c4u, Meshbe meshe, double c1v, double c2v, double c3v, double c4v, int xxx, int yyy)
		{
			double[] temp = new double[4];
			double tmp;

			temp[0] = c1u * meshe.b[xxx, yyy].x + c2u * meshe.b[xxx, yyy + 1].x + c3u * meshe.b[xxx, yyy + 2].x + c4u * meshe.b[xxx, yyy + 3].x;
			temp[1] = c1u * meshe.b[xxx + 1, yyy].x + c2u * meshe.b[xxx + 1, yyy + 1].x + c3u * meshe.b[xxx + 1, yyy + 2].x + c4u * meshe.b[xxx + 1, yyy + 3].x;
			temp[2] = c1u * meshe.b[xxx + 2, yyy].x + c2u * meshe.b[xxx + 2, yyy + 1].x + c3u * meshe.b[xxx + 2, yyy + 2].x + c4u * meshe.b[xxx + 2, yyy + 3].x;
			temp[3] = c1u * meshe.b[xxx + 3, yyy].x + c2u * meshe.b[xxx + 3, yyy + 1].x + c3u * meshe.b[xxx + 3, yyy + 2].x + c4u * meshe.b[xxx + 3, yyy + 3].x;

			tmp = (c1v * temp[0] + c2v * temp[1] + c3v * temp[2] + c4v * temp[3]);

			return tmp;

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rc"></param>
		/// <param name="numc"></param>
		/// <param name="rt"></param>
		/// <param name="numt"></param>
		private void FillTorus(double rc, int numc, double rt, int numt)
		{
			int i, j, k;
			double s = 0, t = 0;
			double pi, twopi;

			pi = 3.14159265358979323846;
			twopi = 2 * pi;

			for (i = 0; i < numc; i++)
			{


				for (j = 1; j <= numt; j++)
				{
					for (k = 1; k >= 0; k--)
					{
						s = (i + k) % numc + 0.5;
						t = j % numt;


					}
					body[pocetbodov].x = (rt + rc * Math.Cos(s * twopi / numc)) * Math.Cos(t * twopi / numt);
					body[pocetbodov].y = (rt + rc * Math.Cos(s * twopi / numc)) * Math.Sin(t * twopi / numt);
					body[pocetbodov++].z = rc * Math.Sin(s * twopi / numc);

				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="i"></param>
		private void Bezier(int i)
		{

			double c1u, c2u, c3u, c4u, c1v, c2v, c3v, c4v, u, v;
			int xxx, yyy;

			for (xxx = 0; xxx <= 4; xxx += 3)
			{
				for (yyy = 0; yyy <= 4; yyy += 3)
				{
					for (u = 0; u <= 1; u += 0.2)
					{

						if (yyy == 3 && u == 0)
							u = 0.2;

						c1u = (1 - u) * (1 - u) * (1 - u);
						c2u = 3 * u * (1 - u) * (1 - u);
						c3u = 3 * u * u * (1 - u);
						c4u = u * u * u;

						for (v = 0; v <= 1; v += 0.2)
						{

							if (xxx == 3 && v == 0)
								v = 0.2;

							c1v = (1 - v) * (1 - v) * (1 - v);
							c2v = 3 * v * (1 - v) * (1 - v);
							c3v = 3 * v * v * (1 - v);
							c4v = v * v * v;

							body[pocetbodov].x = Vynasobxbe(c1u, c2u, c3u, c4u, meshebe[i], c1v, c2v, c3v, c4v, xxx, yyy);
							body[pocetbodov].y = Vynasobybe(c1u, c2u, c3u, c4u, meshebe[i], c1v, c2v, c3v, c4v, xxx, yyy);
							body[pocetbodov++].z = Vynasobzbe(c1u, c2u, c3u, c4u, meshebe[i], c1v, c2v, c3v, c4v, xxx, yyy);

						}
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="i"></param>
		private void Bspline(int i)
		{

			double c1u, c2u, c3u, c4u, c1v, c2v, c3v, c4v, u, v;
			int xxx, yyy;

			for (xxx = 0; xxx <= 1; xxx++)
			{
				for (yyy = 0; yyy <= 1; yyy++)
				{
					for (u = 0; u <= 1; u += 0.2)
					{

						if (yyy == 1 && u == 0)
							u = 0.2;

						c1u = (1 - u) * (1 - u) * (1 - u);
						c2u = 3 * u * u * u - 6 * u * u + 4;
						c3u = -3 * u * u * u + 3 * u * u + 3 * u + 1;
						c4u = u * u * u;

						for (v = 0; v <= 1; v += 0.2)
						{

							if (xxx == 1 && v == 0)
								v = 0.2;

							c1v = (1 - v) * (1 - v) * (1 - v);
							c2v = 3 * v * v * v - 6 * v * v + 4;
							c3v = -3 * v * v * v + 3 * v * v + 3 * v + 1;
							c4v = v * v * v;

							body[pocetbodov].x = Vynasobxbs(c1u, c2u, c3u, c4u, meshebs[i], c1v, c2v, c3v, c4v, xxx, yyy) / 36;
							body[pocetbodov].y = Vynasobybs(c1u, c2u, c3u, c4u, meshebs[i], c1v, c2v, c3v, c4v, xxx, yyy) / 36;
							body[pocetbodov++].z = Vynasobzbs(c1u, c2u, c3u, c4u, meshebs[i], c1v, c2v, c3v, c4v, xxx, yyy) / 36;
						}
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="p"></param>
		/// <returns></returns>
		private double avz(int p)
		{
			double temp;
			temp = (body[plochy[p].bod1].z + body[plochy[p].bod2].z + body[plochy[p].bod3].z);
			return temp;
		}

		/// <summary>
		/// 
		/// </summary>
		private void Usporiadaj()
		{
			int p, i, j;
			int pridal = 0;

			for (p = 0; p < pocetploch; p++)
				uspplochy[p] = -1;
			PocetUspPrivrPloch = 0;

			for (p = 0; p < pocetploch; p++)
			{  // chod cez vsetky plochy

				if (plochy[p].norz > 0)
				{   // vyber len privratene

					for (i = 0; i < PocetUspPrivrPloch; i++)
					{

						if (avz(p) < avz(uspplochy[i]))
						{
							// ak zsuradnica plochy < zsuradnica plochy v poli usporiadanych ploch
							for (j = PocetUspPrivrPloch; j >= i; j--) //urob miesto
								uspplochy[j + 1] = uspplochy[j];
							// a suchni ju tam !
							uspplochy[i] = p;
							pridal = 1;
							break; // vylez z cyklu
						}
					}

					if (i == PocetUspPrivrPloch)
					{
						uspplochy[i] = p;
						pridal = 1;
					}

					if (pridal > 0)
					{
						PocetUspPrivrPloch++;
						pridal = 0;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		private void dajnormalybodom()  // toto je straaasne pomale... Nuz co !
		{
			int kolkympatri, i, j;
			double norz, norx, nory;
			double pod;

			for (i = 0; i < pocetbodov; i++)
			{
				norz = 0.0;
				norx = 0.0;
				nory = 0.0;
				kolkympatri = 0;
				for (j = 0; j < pocetploch; j++)
				{
					if (i == plochy[j].bod1 || i == plochy[j].bod2 || i == plochy[j].bod3)
					{
						norz += plochy[j].norz;
						nory += plochy[j].nory;
						norx += plochy[j].norx;
						kolkympatri++;
					}
				}
				if (kolkympatri > 0)
				{
					pod = Math.Sqrt(norx * norx + nory * nory + norz * norz);
					if (pod != 0)
					{
						body[i].norz = norz / pod;
						body[i].nory = nory / pod;
						body[i].norx = norx / pod;
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void init()
		{
			mbs = mbe = k = t = 0;


			string subor = "tor.dat";

			string[] lines = File.ReadAllLines(subor);
			foreach (string line in lines)
			{
				string[] defs = line.Split(" ");

				if (defs[0] == "kamera")
				{
					kamera.xk = double.Parse(defs[1]);
					kamera.yk = double.Parse(defs[2]);
					kamera.zk = double.Parse(defs[3]);
					kamera.uhol = double.Parse(defs[4]);
				}

				if (defs[0] == "kvader")
				{
					kvadre[k].x = double.Parse(defs[1]);
					kvadre[k].y = double.Parse(defs[2]);
					kvadre[k].z = double.Parse(defs[3]);
					kvadre[k].dx = double.Parse(defs[4]);
					kvadre[k].dy = double.Parse(defs[5]);
					kvadre[k].dz = double.Parse(defs[6]);
					kvadre[k].xuhol = double.Parse(defs[7]);
					kvadre[k].yuhol = double.Parse(defs[8]);
					kvadre[k++].zuhol = double.Parse(defs[9]);
				}

				if (defs[0] == "toroid")
				{
					toroidy[t].x = double.Parse(defs[1]);
					toroidy[t].y = double.Parse(defs[2]);
					toroidy[t].z = double.Parse(defs[3]);
					toroidy[t].rin = double.Parse(defs[4]);
					toroidy[t].rout = double.Parse(defs[5]);
					toroidy[t].xjemnost = int.Parse(defs[6]);
					toroidy[t].yjemnost = int.Parse(defs[7]);
					toroidy[t].xuhol = double.Parse(defs[8]);
					toroidy[t].yuhol = double.Parse(defs[9]);
					toroidy[t++].zuhol = double.Parse(defs[10]);
				}

				if (defs[0] == "koniec")
				{
					break;
				}
			}

				/*
				puts("zadaj subor");
				gets(subor);

				vstup.open(subor);
				if (vstup.good()) {

					while (vstup.good()) {
						vstup >> prvok;         // nacitaj meno objektu

						if (!stricmp(prvok, "kamera")) {
							vstup >> kamera.xk >> kamera.yk >> kamera.zk >> kamera.uhol;
						}
						else


						if (!stricmp(prvok, "kvader")) {    // tu sa budu citat params pre kvader
							vstup >> kvadre[k].x;
							vstup >> kvadre[k].y;
							vstup >> kvadre[k].z;
							vstup >> kvadre[k].dx;
							vstup >> kvadre[k].dy;
							vstup >> kvadre[k].dz;
							vstup >> kvadre[k].xuhol;
							vstup >> kvadre[k].yuhol;
							vstup >> kvadre[k++].zuhol;
						}
						else

						if (!stricmp(prvok, "toroid")) {    // tu sa budu citat params pre toroid
							vstup >> toroidy[t].x;
							vstup >> toroidy[t].y;
							vstup >> toroidy[t].z;
							vstup >> toroidy[t].rin;
							vstup >> toroidy[t].rout;
							vstup >> toroidy[t].xjemnost;
							vstup >> toroidy[t].yjemnost;
							vstup >> toroidy[t].xuhol;
							vstup >> toroidy[t].yuhol;
							vstup >> toroidy[t++].zuhol;
						}
						else

							if (!stricmp(prvok, "meshbs")) {    // tu sa budu citat params pre toroid
							vstup >> meshebs[mbs].x;
							vstup >> meshebs[mbs].y;
							vstup >> meshebs[mbs].z;
							vstup >> meshebs[mbs].xuhol;
							vstup >> meshebs[mbs].yuhol;
							vstup >> meshebs[mbs].zuhol;
							for (int k = 0; k < 5; k++)
								for (int l = 0; l < 5; l++)
								{
									vstup >> meshebs[mbs].b[l][k].x;
									vstup >> meshebs[mbs].b[l][k].y;
									vstup >> meshebs[mbs].b[l][k].z;
								}
							mbs++;
						}

						else
							if (!stricmp(prvok, "meshbe")) {    // tu sa budu citat params pre toroid
							vstup >> meshebe[mbe].x;
							vstup >> meshebe[mbe].y;
							vstup >> meshebe[mbe].z;
							vstup >> meshebe[mbe].xuhol;
							vstup >> meshebe[mbe].yuhol;
							vstup >> meshebe[mbe].zuhol;
							for (int k = 0; k < 7; k++)
								for (int l = 0; l < 7; l++)
								{
									vstup >> meshebe[mbe].b[l][k].x;
									vstup >> meshebe[mbe].b[l][k].y;
									vstup >> meshebe[mbe].b[l][k].z;
								}
							mbe++;
						}
						else

						if (!stricmp(prvok, "koniec")) {
							vstup.close();
							break;
						}

						else {
							puts("Chyba ! Neznamy prvok !");
							vstup.close();
							exit(0);
						}
					}
				}

				printf("Nacitalo sa %d kvadrov, %d toroidov a %d ploch spline a %d ploch bezier \n", k, t, mbs, mbe);
				getchar();
				*/

			int i, j;

			for (i = 0; i < k; i++)              //vyroba hran kociek
			{
				int ptemp = pocetbodov;

				double px = kvadre[i].dx / 2;
				double py = kvadre[i].dy / 2;
				double pz = kvadre[i].dz / 2;

				body[pocetbodov].x = px;
				body[pocetbodov].y = py;
				body[pocetbodov++].z = pz;

				body[pocetbodov].x = -px;
				body[pocetbodov].y = py;
				body[pocetbodov++].z = pz;

				body[pocetbodov].x = -px;
				body[pocetbodov].y = py;
				body[pocetbodov++].z = -pz;

				body[pocetbodov].x = px;
				body[pocetbodov].y = py;
				body[pocetbodov++].z = -pz;

				body[pocetbodov].x = px;
				body[pocetbodov].y = -py;
				body[pocetbodov++].z = pz;

				body[pocetbodov].x = -px;
				body[pocetbodov].y = -py;
				body[pocetbodov++].z = pz;

				body[pocetbodov].x = -px;
				body[pocetbodov].y = -py;
				body[pocetbodov++].z = -pz;

				body[pocetbodov].x = px;
				body[pocetbodov].y = -py;
				body[pocetbodov++].z = -pz;


				for (j = pocetbodov - 8; j < pocetbodov; j++)
				{             //rotacia bodov okolo z
					x1 = body[j].x * Math.Cos(kvadre[i].zuhol) - body[j].y * Math.Sin(kvadre[i].zuhol);
					y1 = body[j].x * Math.Sin(kvadre[i].zuhol) + body[j].y * Math.Cos(kvadre[i].zuhol);
					body[j].x = x1;
					body[j].y = y1;
				}

				for (j = pocetbodov - 8; j < pocetbodov; j++)
				{             //rotacia bodov okolo y
					x1 = body[j].x * Math.Cos(kvadre[i].yuhol) - body[j].z * Math.Sin(kvadre[i].yuhol);
					z1 = body[j].x * Math.Sin(kvadre[i].yuhol) + body[j].z * Math.Cos(kvadre[i].yuhol);
					body[j].x = x1;
					body[j].z = z1;
				}

				for (j = pocetbodov - 8; j < pocetbodov; j++)
				{             //rotacia bodov hran okolo z
					z1 = body[j].z * Math.Cos(kvadre[i].xuhol) - body[j].y * Math.Sin(kvadre[i].xuhol);
					y1 = body[j].z * Math.Sin(kvadre[i].xuhol) + body[j].y * Math.Cos(kvadre[i].xuhol);
					body[j].z = z1;
					body[j].y = y1;

				}

				for (j = pocetbodov - 8; j < pocetbodov; j++)
				{             //rotacia bodov hran okolo z
					body[j].x += kvadre[i].x;
					body[j].y += kvadre[i].y;
					body[j].z += kvadre[i].z;
				}

				for (j = 0; j < 4; j++)
				{
					hrany[pocethran].bod1 = ptemp + j;
					hrany[pocethran++].bod2 = ptemp + j + 4;
					hrany[pocethran].bod1 = ptemp + j;
					if (j != 3)
						hrany[pocethran++].bod2 = ptemp + j + 5;
					else
						hrany[pocethran++].bod2 = ptemp + j + 1;
				}
				for (j = 0; j < 3; j++)
				{
					hrany[pocethran].bod1 = ptemp + j;
					hrany[pocethran++].bod2 = ptemp + j + 1;
				}
				hrany[pocethran].bod1 = ptemp + 3;
				hrany[pocethran++].bod2 = ptemp;
				for (j = 0; j < 4; j++)
				{
					hrany[pocethran].bod1 = ptemp + j + 4;
					if (j != 3)
						hrany[pocethran++].bod2 = ptemp + j + 5;
					else
						hrany[pocethran++].bod2 = ptemp + j + 1;
				}

				hrany[pocethran].bod1 = ptemp;
				hrany[pocethran++].bod2 = ptemp + 2;
				hrany[pocethran].bod1 = ptemp + 5;
				hrany[pocethran++].bod2 = ptemp + 7;


				for (j = 0; j < 3; j++)
				{
					plochy[pocetploch].bod1 = ptemp + j;
					plochy[pocetploch].bod2 = ptemp + j + 1;
					plochy[pocetploch].bod3 = ptemp + j + 5;
					plochy[pocetploch].zaobli = 0;
					normalizuj(pocetploch++);
				}

				plochy[pocetploch].bod3 = ptemp + 3;
				plochy[pocetploch].bod2 = ptemp + 3 + 1;
				plochy[pocetploch].bod1 = ptemp;
				plochy[pocetploch].zaobli = 0;
				normalizuj(pocetploch++);



				for (j = 4; j < 7; j++)
				{
					plochy[pocetploch].bod2 = ptemp + j;
					plochy[pocetploch].bod1 = ptemp + j + 1;
					plochy[pocetploch].bod3 = ptemp + j - 4;
					plochy[pocetploch].zaobli = 0;
					normalizuj(pocetploch++);
				}
				plochy[pocetploch].bod2 = ptemp + 7;
				plochy[pocetploch].bod1 = ptemp + 4;
				plochy[pocetploch].bod3 = ptemp + 3;
				plochy[pocetploch].zaobli = 0;
				normalizuj(pocetploch++);

				plochy[pocetploch].bod2 = ptemp;
				plochy[pocetploch].bod1 = ptemp + 1;
				plochy[pocetploch].bod3 = ptemp + 2;
				plochy[pocetploch].zaobli = 0;
				normalizuj(pocetploch++);

				plochy[pocetploch].bod2 = ptemp;
				plochy[pocetploch].bod1 = ptemp + 2;
				plochy[pocetploch].bod3 = ptemp + 3;
				plochy[pocetploch].zaobli = 0;
				normalizuj(pocetploch++);

				plochy[pocetploch].bod1 = 4 + ptemp;
				plochy[pocetploch].bod2 = 4 + ptemp + 1;
				plochy[pocetploch].bod3 = 4 + ptemp + 2;
				plochy[pocetploch].zaobli = 0;
				normalizuj(pocetploch++);

				plochy[pocetploch].bod1 = 4 + ptemp;
				plochy[pocetploch].bod2 = 4 + ptemp + 2;
				plochy[pocetploch].bod3 = 4 + ptemp + 3;
				plochy[pocetploch].zaobli = 0;
				normalizuj(pocetploch++);

			}

			//vyroba hran toroidov
			for (i = 0; i < t; i++)
			{
				int ptemp = pocetbodov;
				FillTorus(toroidy[i].rin, toroidy[i].xjemnost, toroidy[i].rout, toroidy[i].yjemnost);

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov okolo z
					x1 = body[j].x * Math.Cos(toroidy[i].zuhol) - body[j].y * Math.Sin(toroidy[i].zuhol);
					y1 = body[j].x * Math.Sin(toroidy[i].zuhol) + body[j].y * Math.Cos(toroidy[i].zuhol);
					body[j].x = x1;
					body[j].y = y1;
				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov okolo y
					x1 = body[j].x * Math.Cos(toroidy[i].yuhol) - body[j].z * Math.Sin(toroidy[i].yuhol);
					z1 = body[j].x * Math.Sin(toroidy[i].yuhol) + body[j].z * Math.Cos(toroidy[i].yuhol);
					body[j].x = x1;
					body[j].z = z1;
				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov hran okolo z
					z1 = body[j].z * Math.Cos(toroidy[i].xuhol) - body[j].y * Math.Sin(toroidy[i].xuhol);
					y1 = body[j].z * Math.Sin(toroidy[i].xuhol) + body[j].y * Math.Cos(toroidy[i].xuhol);
					body[j].z = z1;
					body[j].y = y1;
				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //posun bodov hran
					body[j].x += toroidy[i].x;
					body[j].y += toroidy[i].y;
					body[j].z += toroidy[i].z;
				}

				int xj = toroidy[i].xjemnost;
				int yj = toroidy[i].yjemnost;

				for (j = 0; j < xj; j++)
				{
					for (ii = 0; ii < yj; ii++)
					{
						hrany[pocethran].bod1 = ii % yj + (j % xj) * yj + ptemp;
						hrany[pocethran++].bod2 = ii % yj + ((j + 1) % xj) * yj + ptemp;
					}
				}

				for (j = 0; j < xj; j++)
				{
					for (ii = 0; ii < yj; ii++)
					{
						hrany[pocethran].bod1 = ii % yj + (j % xj) * yj + ptemp;
						hrany[pocethran++].bod2 = (ii + 1) % yj + yj * (j % xj) + ptemp;
					}
				}

				for (j = 0; j < xj; j++)
				{
					for (ii = 0; ii < yj; ii++)
					{
						hrany[pocethran].bod1 = ii % yj + yj * ((j + 1) % xj) + ptemp;
						hrany[pocethran++].bod2 = (ii + 1) % yj + (j % xj) * yj + ptemp;
					}
				}

				for (j = 0; j < xj; j++)
				{
					for (ii = 0; ii < yj; ii++)
					{
						plochy[pocetploch].bod1 = ptemp + ii % yj + (j % xj) * yj;
						plochy[pocetploch].bod2 = ptemp + (ii + 1) % yj + (j % xj) * yj;
						plochy[pocetploch].bod3 = ptemp + ii % yj + ((j + 1) % xj) * yj;
						plochy[pocetploch].zaobli = 1;
						normalizuj(pocetploch++);
					}
				}

				for (j = 1; j <= xj; j++)
				{
					for (ii = 1; ii <= yj; ii++)
					{
						plochy[pocetploch].bod1 = ptemp + ii % yj + (j % xj) * yj;
						plochy[pocetploch].bod2 = ptemp + (ii - 1) % yj + (j % xj) * yj;
						plochy[pocetploch].bod3 = ptemp + ii % yj + ((j - 1) % xj) * yj;
						plochy[pocetploch].zaobli = 1;
						normalizuj(pocetploch++);
					}
				}
			}

			//vyroba hran meshov
			for (i = 0; i < mbs; i++)
			{

				int ptemp = pocetbodov;

				Bspline(i);


				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov okolo z
					x1 = body[j].x * Math.Cos(meshebs[i].zuhol) - body[j].y * Math.Sin(meshebs[i].zuhol);
					y1 = body[j].x * Math.Sin(meshebs[i].zuhol) + body[j].y * Math.Cos(meshebs[i].zuhol);
					body[j].x = x1;
					body[j].y = y1;
				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov okolo y
					x1 = body[j].x * Math.Cos(meshebs[i].yuhol) - body[j].z * Math.Sin(meshebs[i].yuhol);
					z1 = body[j].x * Math.Sin(meshebs[i].yuhol) + body[j].z * Math.Cos(meshebs[i].yuhol);
					body[j].x = x1;
					body[j].z = z1;
				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov hran okolo z
					z1 = body[j].z * Math.Cos(meshebs[i].xuhol) - body[j].y * Math.Sin(meshebs[i].xuhol);
					y1 = body[j].z * Math.Sin(meshebs[i].xuhol) + body[j].y * Math.Cos(meshebs[i].xuhol);
					body[j].z = z1;
					body[j].y = y1;

				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //posun bodov hran
					body[j].x += meshebs[i].x;
					body[j].y += meshebs[i].y;
					body[j].z += meshebs[i].z;
				}

				for (j = 0; j < 11; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{

						hrany[pocethran].bod1 = ptemp + ii + j * 6;
						hrany[pocethran++].bod2 = ptemp + ii + 1 + j * 6;

						if (j != 10)
						{
							hrany[pocethran].bod1 = ptemp + ii + j * 6;
							hrany[pocethran++].bod2 = ptemp + ii + (j + 1) * 6;

							hrany[pocethran].bod1 = ptemp + ii + 1 + j * 6;
							hrany[pocethran++].bod2 = ptemp + ii + (j + 1) * 6;
						}
					}
				}

				for (j = 0; j < 11; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{
						if (ii == 0)
						{
							hrany[pocethran].bod1 = ptemp + 5 + j * 6;
							hrany[pocethran++].bod2 = ptemp + 65 + ii + 1 + j * 5;
							if (j != 10)
							{
								hrany[pocethran].bod1 = ptemp + 65 + ii + 1 + j * 5;
								hrany[pocethran++].bod2 = ptemp + 5 + (j + 1) * 6;

								hrany[pocethran].bod1 = ptemp + 5 + j * 6;
								hrany[pocethran++].bod2 = ptemp + 5 + (j + 1) * 6;
							}

						}
						else
						{

							hrany[pocethran].bod1 = ptemp + 65 + ii + j * 5;
							hrany[pocethran++].bod2 = ptemp + 65 + ii + 1 + j * 5;

							if (j != 10)
							{
								hrany[pocethran].bod1 = ptemp + 65 + ii + 1 + j * 5;
								hrany[pocethran++].bod2 = ptemp + 65 + ii + (j + 1) * 5;

								hrany[pocethran].bod1 = ptemp + 65 + ii + j * 5;
								hrany[pocethran++].bod2 = ptemp + 65 + ii + (j + 1) * 5;
							}
						}
					}
				}


				for (j = 0; j < 10; j++)
				{
					hrany[pocethran].bod1 = ptemp + 65 + 5 + j * 5;
					hrany[pocethran++].bod2 = ptemp + 65 + 5 + (j + 1) * 5;
				}

				for (j = 0; j < 10; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{
						plochy[pocetploch].bod1 = ptemp + ii + j * 6;
						plochy[pocetploch].bod2 = ptemp + ii + 1 + j * 6;
						plochy[pocetploch].bod3 = ptemp + ii + (j + 1) * 6;
						plochy[pocetploch].zaobli = 1;
						normalizuj(pocetploch++);
					}
				}

				for (j = 1; j < 11; j++)
				{
					for (ii = 1; ii < 6; ii++)
					{
						plochy[pocetploch].bod1 = ptemp + ii + j * 6;
						plochy[pocetploch].bod2 = ptemp + ii - 1 + j * 6;
						plochy[pocetploch].bod3 = ptemp + ii + (j - 1) * 6;
						plochy[pocetploch].zaobli = 1;
						normalizuj(pocetploch++);
					}
				}


				for (j = 0; j < 10; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{
						if (ii == 0)
						{
							plochy[pocetploch].bod1 = ptemp + 5 + j * 6;
							plochy[pocetploch].bod2 = ptemp + 65 + ii + 1 + j * 5;
							plochy[pocetploch].bod3 = ptemp + 5 + (j + 1) * 6;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
						else
						{

							plochy[pocetploch].bod1 = ptemp + 65 + ii + j * 5;
							plochy[pocetploch].bod2 = ptemp + 65 + ii + 1 + j * 5;
							plochy[pocetploch].bod3 = ptemp + 65 + ii + (j + 1) * 5;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
					}
				}

				for (j = 1; j < 11; j++)
				{
					for (ii = 1; ii < 6; ii++)
					{
						if (ii == 1)
						{
							plochy[pocetploch].bod1 = ptemp + 65 + ii + j * 5;
							plochy[pocetploch].bod2 = ptemp + 5 + ii - 1 + j * 6;
							plochy[pocetploch].bod3 = ptemp + 65 + ii + (j - 1) * 5;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
						else
						{

							plochy[pocetploch].bod1 = ptemp + 65 + ii + j * 5;
							plochy[pocetploch].bod2 = ptemp + 65 + ii - 1 + j * 5;
							plochy[pocetploch].bod3 = ptemp + 65 + ii + (j - 1) * 5;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
					}
				}
			}

			for (i = 0; i < mbe; i++)
			{

				int ptemp = pocetbodov;

				Bezier(i);

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov okolo z
					x1 = body[j].x * Math.Cos(meshebe[i].zuhol) - body[j].y * Math.Sin(meshebe[i].zuhol);
					y1 = body[j].x * Math.Sin(meshebe[i].zuhol) + body[j].y * Math.Cos(meshebe[i].zuhol);
					body[j].x = x1;
					body[j].y = y1;
				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov okolo y
					x1 = body[j].x * Math.Cos(meshebe[i].yuhol) - body[j].z * Math.Sin(meshebe[i].yuhol);
					z1 = body[j].x * Math.Sin(meshebe[i].yuhol) + body[j].z * Math.Cos(meshebe[i].yuhol);
					body[j].x = x1;
					body[j].z = z1;
				}

				for (j = ptemp; j < pocetbodov; j++)
				{             //rotacia bodov hran okolo z
					z1 = body[j].z * Math.Cos(meshebe[i].xuhol) - body[j].y * Math.Sin(meshebe[i].xuhol);
					y1 = body[j].z * Math.Sin(meshebe[i].xuhol) + body[j].y * Math.Cos(meshebe[i].xuhol);
					body[j].z = z1;
					body[j].y = y1;

				}


				for (j = ptemp; j < pocetbodov; j++)
				{             //posun bodov hran
					body[j].x += meshebe[i].x;
					body[j].y += meshebe[i].y;
					body[j].z += meshebe[i].z;
				}


				for (j = 0; j < 11; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{

						hrany[pocethran].bod1 = ptemp + ii + j * 6;
						hrany[pocethran++].bod2 = ptemp + ii + 1 + j * 6;

						if (j != 10)
						{
							hrany[pocethran].bod1 = ptemp + ii + j * 6;
							hrany[pocethran++].bod2 = ptemp + ii + (j + 1) * 6;

							hrany[pocethran].bod1 = ptemp + ii + 1 + j * 6;
							hrany[pocethran++].bod2 = ptemp + ii + (j + 1) * 6;
						}
					}
				}

				for (j = 0; j < 11; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{
						if (ii == 0)
						{
							hrany[pocethran].bod1 = ptemp + 5 + j * 6;
							hrany[pocethran++].bod2 = ptemp + 65 + ii + 1 + j * 5;
							if (j != 10)
							{
								hrany[pocethran].bod1 = ptemp + 65 + ii + 1 + j * 5;
								hrany[pocethran++].bod2 = ptemp + 5 + (j + 1) * 6;

								hrany[pocethran].bod1 = ptemp + 5 + j * 6;
								hrany[pocethran++].bod2 = ptemp + 5 + (j + 1) * 6;
							}

						}
						else
						{

							hrany[pocethran].bod1 = ptemp + 65 + ii + j * 5;
							hrany[pocethran++].bod2 = ptemp + 65 + ii + 1 + j * 5;

							if (j != 10)
							{
								hrany[pocethran].bod1 = ptemp + 65 + ii + 1 + j * 5;
								hrany[pocethran++].bod2 = ptemp + 65 + ii + (j + 1) * 5;

								hrany[pocethran].bod1 = ptemp + 65 + ii + j * 5;
								hrany[pocethran++].bod2 = ptemp + 65 + ii + (j + 1) * 5;
							}
						}
					}
				}


				for (j = 0; j < 10; j++)
				{
					hrany[pocethran].bod1 = ptemp + 65 + 5 + j * 5;
					hrany[pocethran++].bod2 = ptemp + 65 + 5 + (j + 1) * 5;
				}

				for (j = 0; j < 10; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{
						plochy[pocetploch].bod1 = ptemp + ii + j * 6;
						plochy[pocetploch].bod2 = ptemp + ii + 1 + j * 6;
						plochy[pocetploch].bod3 = ptemp + ii + (j + 1) * 6;
						plochy[pocetploch].zaobli = 1;
						normalizuj(pocetploch++);
					}
				}
				for (j = 1; j < 11; j++)
				{
					for (ii = 1; ii < 6; ii++)
					{
						plochy[pocetploch].bod1 = ptemp + ii + j * 6;
						plochy[pocetploch].bod2 = ptemp + ii - 1 + j * 6;
						plochy[pocetploch].bod3 = ptemp + ii + (j - 1) * 6;
						plochy[pocetploch].zaobli = 1;
						normalizuj(pocetploch++);
					}
				}


				for (j = 0; j < 10; j++)
				{
					for (ii = 0; ii < 5; ii++)
					{
						if (ii == 0)
						{
							plochy[pocetploch].bod1 = ptemp + 5 + j * 6;
							plochy[pocetploch].bod2 = ptemp + 65 + ii + 1 + j * 5;
							plochy[pocetploch].bod3 = ptemp + 5 + (j + 1) * 6;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
						else
						{

							plochy[pocetploch].bod1 = ptemp + 65 + ii + j * 5;
							plochy[pocetploch].bod2 = ptemp + 65 + ii + 1 + j * 5;
							plochy[pocetploch].bod3 = ptemp + 65 + ii + (j + 1) * 5;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
					}
				}

				for (j = 1; j < 11; j++)
				{
					for (ii = 1; ii < 6; ii++)
					{
						if (ii == 1)
						{
							plochy[pocetploch].bod1 = ptemp + 65 + ii + j * 5;
							plochy[pocetploch].bod2 = ptemp + 5 + ii - 1 + j * 6;
							plochy[pocetploch].bod3 = ptemp + 65 + ii + (j - 1) * 5;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
						else
						{

							plochy[pocetploch].bod1 = ptemp + 65 + ii + j * 5;
							plochy[pocetploch].bod2 = ptemp + 65 + ii - 1 + j * 5;
							plochy[pocetploch].bod3 = ptemp + 65 + ii + (j - 1) * 5;
							plochy[pocetploch].zaobli = 1;
							normalizuj(pocetploch++);
						}
					}
				}
			}

			dajnormalybodom();
			for (j = 0; j < pocetploch; j++)
			{
				normalizuj(j);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public void main(char what, PictureBox picture)
		{
			//*************************** Tu sa zacina zobrazovanie *************

		zac:

			//fflush(stdin);

			//printf("bodov : %d\nhran : %d\nploch : %d\n\n", pocetbodov, pocethran, pocetploch);

			//puts("Toto je menu. Vyber si ...");
			//puts("1...body");
			//puts("2...vsetky hrany");
			//puts("3...privratene plochy konstantne tienovane");
			//puts("4...(3) plus maliarov algoritmus");
			//puts("5...Privratene plochy maliarov algoritmus Gouraudovo tienovanie");
			//puts("6...koniec");

			//ch = getch();
			ch = what;


		//if (ch != '1')
		//	if (ch != '2')
		//		if (ch != '3')
		//			if (ch != '4')
		//				if (ch != '5')
		//				{
		//					//exit(0);
		//				}

		////initSuperVGA(true);

		//if (maxpage != 0)
		//{
		//	vpage = 1;
		//	apage = 0;
		//	//setActivePage(apage);
		//	//setVisualPage(apage);
		//}

		zaciatok:

			var g = Graphics.FromImage(db.Bitmap);
			g.FillRectangle(Brushes.Black, new Rectangle(0, 0, 1000, 800));

			//setActivePage(apage);
			//clear(0);

			if (ch == '1')
			{
				for (int j = 0; j < pocetbodov; j++)
				{   // chod cez fsetky body

					xo1 = (int)(100 / kamera.zk * (body[j].x + body[j].z / 4) + xres / 2 + 100 / kamera.zk * kamera.xk);
					yo1 = (int)(100 / kamera.zk * (body[j].y + body[j].z / 4) + yres / 2 + 100 / kamera.zk * kamera.yk);

					db.SetPixel(xo1, yo1, Color.FromArgb(255, 255, 0, 0));

					//GDI.SetPixel(hdc, xo1, yo1, 255, 0, 0);
					//putPixel(xo1, yo1, rgbColor((uchar)(255), 0, 0));
				}
			}

			if (ch == '2')
			{
				for (int j = 0; j < pocethran; j++)
				{   // chod cez fsetky plochy
					xo1 = (int)(100 / kamera.zk * (body[hrany[j].bod1].x + body[hrany[j].bod1].z / 4) + xres / 2 + 100 / kamera.zk * kamera.xk);
					yo1 = (int)(100 / kamera.zk * (body[hrany[j].bod1].y + body[hrany[j].bod1].z / 4) + yres / 2 + 100 / kamera.zk * kamera.yk);
					xo2 = (int)(100 / kamera.zk * (body[hrany[j].bod2].x + body[hrany[j].bod2].z / 4) + xres / 2 + 100 / kamera.zk * kamera.xk);
					yo2 = (int)(100 / kamera.zk * (body[hrany[j].bod2].y + body[hrany[j].bod2].z / 4) + yres / 2 + 100 / kamera.zk * kamera.yk);

					pen.Color = Color.FromArgb(255,255,0,0);
					g.DrawLine(pen, xo1, yo1, xo2, yo2);
					//line(xo1, yo1, xo2, yo2, rgbColor((uchar)(255), 0, 0));
				}
			}

			if (ch == '3')
			{
				for (int j = 0; j < pocetploch; j++)
				{   // chod cez fsetky plochy
					if (plochy[j].norz > 0)
					{


						long x1, x2, x3, y1, y2, y3;
						long xs;
						long xl, xp;
						long xx, yy, ind;
						long tmp;

						tmp = (long)(110 / kamera.zk);

						x1 = (long)(tmp * (body[plochy[j].bod1].x + body[plochy[j].bod1].z / 4) + xres / 2 + tmp * kamera.xk);
						y1 = (long)(tmp * (body[plochy[j].bod1].y + body[plochy[j].bod1].z / 4) + yres / 2 + tmp * kamera.yk);
						x2 = (long)(tmp * (body[plochy[j].bod2].x + body[plochy[j].bod2].z / 4) + xres / 2 + tmp * kamera.xk);
						y2 = (long)(tmp * (body[plochy[j].bod2].y + body[plochy[j].bod2].z / 4) + yres / 2 + tmp * kamera.yk);
						x3 = (long)(tmp * (body[plochy[j].bod3].x + body[plochy[j].bod3].z / 4) + xres / 2 + tmp * kamera.xk);
						y3 = (long)(tmp * (body[plochy[j].bod3].y + body[plochy[j].bod3].z / 4) + yres / 2 + tmp * kamera.yk);

						// tu zacina vyplnanie 1 plochy
						ind = (long)(255 * plochy[j].norz);


						if (y1 > y2)
						{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; }
						if (y2 > y3)
						{ tmp = y2; y2 = y3; y3 = tmp; tmp = x2; x2 = x3; x3 = tmp; }
						if (y1 > y2)
						{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; }
						// toto bolo usporaduvanie podla y suradnice.


						if (Math.Abs(y3 - y1) > 1)
						{

							// najdenie pomocneho bodu
							xs = (x1 * (y2 - y3) - x3 * (y2 - y1)) / (y1 - y3);

							for (yy = y1; yy <= y2; yy++)
							{ // horny trojuholnik

								if (Math.Abs(y1 - y2) < 1)
								{
									xl = x1;
									xp = x2;
								}
								else
								{
									xl = (x1 * (yy - y2) - x2 * (yy - y1)) / (y1 - y2);
									xp = (x1 * (yy - y2) - xs * (yy - y1)) / (y1 - y2);
								}

								pen.Color = Color.FromArgb(255, (int)ind, 0, 0);
								g.DrawLine(pen, xl, yy, xp, yy);

								//Graphics.DrawLine(pen, xl, yy, xp, yy);
								//line(xl, yy, xp, yy, rgbColor((uchar)(ind), 0, 0));
							}

							for (yy = y3; yy >= y2; yy--)
							{ // dolny trojuholnik

								if (Math.Abs(y3 - y2) < 1)
								{
									xl = x2;
									xp = x3;
								}
								else
								{
									xl = (x3 * (yy - y2) - x2 * (yy - y3)) / (y3 - y2);
									xp = (x3 * (yy - y2) - xs * (yy - y3)) / (y3 - y2);
								}

								pen.Color = Color.FromArgb(255, (int)ind, 0, 0);
								g.DrawLine(pen, xl, yy, xp, yy);
								//line(xl, yy, xp, yy, rgbColor((uchar)(ind), 0, 0));

							}
						}
					}
				}
			}

			if (ch == '4')
			{
				Usporiadaj(); // <------- Maliars algorithmusz
				for (int j = 0; j < PocetUspPrivrPloch; j++)
				{   // chod cez fsetky plochy

					long x1, x2, x3, y1, y2, y3;
					long xs;
					long xl, xp;
					long xx, yy, ind;
					long tmp;


					x1 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod1].x + 0.25 * body[plochy[uspplochy[j]].bod1].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
					y1 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod1].y + 0.25 * body[plochy[uspplochy[j]].bod1].z) + yres / 2 + 100 / kamera.zk * kamera.yk);
					x2 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod2].x + 0.25 * body[plochy[uspplochy[j]].bod2].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
					y2 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod2].y + 0.25 * body[plochy[uspplochy[j]].bod2].z) + yres / 2 + 100 / kamera.zk * kamera.yk);
					x3 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod3].x + 0.25 * body[plochy[uspplochy[j]].bod3].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
					y3 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod3].y + 0.25 * body[plochy[uspplochy[j]].bod3].z) + yres / 2 + 100 / kamera.zk * kamera.yk);

					// tu zacina vyplnanie 1 plochy
					ind = (long)(255 * plochy[uspplochy[j]].norz);


					if (y1 > y2)
					{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; }
					if (y2 > y3)
					{ tmp = y2; y2 = y3; y3 = tmp; tmp = x2; x2 = x3; x3 = tmp; }
					if (y1 > y2)
					{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; }
					// toto bolo usporaduvanie podla y suradnice.


					if (Math.Abs(y3 - y1) > 1)
					{
						// najdenie pomocneho bodu
						xs = (x1 * (y2 - y3) - x3 * (y2 - y1)) / (y1 - y3);
						for (yy = y1; yy <= y2; yy++)  // horny trojuholnik
						{
							if (Math.Abs(y1 - y2) < 1)
							{
								xl = x1;
								xp = x2;
							}
							else
							{
								xl = (x1 * (yy - y2) - x2 * (yy - y1)) / (y1 - y2);
								xp = (x1 * (yy - y2) - xs * (yy - y1)) / (y1 - y2);
							}

							pen.Color = Color.FromArgb(255, (int)ind, 0, 0);
							g.DrawLine(pen, xl, yy, xp, yy);
							//Graphics.DrawLine(pen, xl, yy, xp, yy);
							//line(xl,yy,xp,yy,rgbColor((uchar)(ind),0,0));
						}


						for (yy = y3; yy >= y2; yy--)
						{ // dolny trojuholnik

							if (Math.Abs(y3 - y2) < 1)
							{
								xl = x2;
								xp = x3;
							}
							else
							{
								xl = (x3 * (yy - y2) - x2 * (yy - y3)) / (y3 - y2);
								xp = (x3 * (yy - y2) - xs * (yy - y3)) / (y3 - y2);
							}

							pen.Color = Color.FromArgb(255, (int)ind, 0, 0);
							g.DrawLine(pen, xl, yy, xp, yy);
							//Graphics.DrawLine(pen, xl, yy, xp, yy);
							//line(xl,yy,xp,yy,rgbColor((uchar)(ind),0,0));

						}

					}

				}
			}

			if (ch == '5')
			{

				Usporiadaj();
				for (int j = 0; j < PocetUspPrivrPloch; j++)
				{   // chod cez fsetky plochy

					if (plochy[uspplochy[j]].zaobli > 0)
					{


						long x1, x2, x3, y1, y2, y3;
						long xs;
						long xl, xp;
						long i1, i2, i3, isd, il, ip;
						long ind;
						long xx, yy;
						long tmp;

						x1 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod1].x + 0.25 * body[plochy[uspplochy[j]].bod1].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
						y1 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod1].y + 0.25 * body[plochy[uspplochy[j]].bod1].z) + yres / 2 + 100 / kamera.zk * kamera.yk);
						x2 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod2].x + 0.25 * body[plochy[uspplochy[j]].bod2].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
						y2 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod2].y + 0.25 * body[plochy[uspplochy[j]].bod2].z) + yres / 2 + 100 / kamera.zk * kamera.yk);
						x3 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod3].x + 0.25 * body[plochy[uspplochy[j]].bod3].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
						y3 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod3].y + 0.25 * body[plochy[uspplochy[j]].bod3].z) + yres / 2 + 100 / kamera.zk * kamera.yk);

						// tu zacina Gouraudovanie 1 plochy


						if (body[plochy[uspplochy[j]].bod1].norz > 0)
							i1 = (long)(body[plochy[uspplochy[j]].bod1].norz * 255);
						else
							i1 = 0;

						if (body[plochy[uspplochy[j]].bod2].norz > 0)
							i2 = (long)(body[plochy[uspplochy[j]].bod2].norz * 255);
						else
							i2 = 0;

						if (body[plochy[uspplochy[j]].bod3].norz > 0)
							i3 = (long)(body[plochy[uspplochy[j]].bod3].norz * 255);
						else
							i3 = 0;
						//toto bolo najdenie farieb vo vrcholoch...


						if (y1 > y2)
						{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; tmp = i1; i1 = i2; i2 = tmp; }
						if (y2 > y3)
						{ tmp = y2; y2 = y3; y3 = tmp; tmp = x2; x2 = x3; x3 = tmp; tmp = i2; i2 = i3; i3 = tmp; }
						if (y1 > y2)
						{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; tmp = i1; i1 = i2; i2 = tmp; }
						// toto bolo usporaduvanie podla y suradnice.


						if (y3 - y1 > 1)
						{

							// najdenie pomocneho bodu
							xs = (x1 * (y2 - y3) - x3 * (y2 - y1)) / (y1 - y3);
							isd = (i1 * (y2 - y3) - i3 * (y2 - y1)) / (y1 - y3);

							dy = (int)(y1 - y2);

							for (yy = y1; yy <= y2; yy++)
							{ // horny trojuholnik

								if ((dy == 0))
								{
									xl = x1;
									xp = x2;
									il = i1;
									ip = i2;
								}
								else
								{
									xl = (x1 * (yy - y2) - x2 * (yy - y1)) / dy;
									xp = (x1 * (yy - y2) - xs * (yy - y1)) / dy;
									il = (i1 * (yy - y2) - i2 * (yy - y1)) / dy;
									ip = (i1 * (yy - y2) - isd * (yy - y1)) / dy;
								}

								if (xl > xp)
								{
									tmp = xl;
									xl = xp;
									xp = tmp;
									tmp = il;
									il = ip;
									ip = tmp;
								}

								dx = (int)(xl - xp);

								for (xx = xl; xx <= xp; xx++)
								{// podho po riadku !
									if ((dx == 0))
										ind = il;
									else
										ind = (il * (xx - xp) - ip * (xx - xl)) / dx;

									db.SetPixel((int)xx, (int)yy, Color.FromArgb(255, (int)ind, 0, 0));
									//GDI.SetPixel(hdc, xx, yy, rgbColor((uchar)(ind), 0, 0));
									//putPixel(xx,yy,rgbColor((uchar)(ind),0,0));
								}
							}

							dy = (int)(y3 - y2);

							for (yy = y3; yy >= y2; yy--)
							{ // dolny trojuholnik

								if ((dy == 0))
								{
									xl = x2;
									xp = x3;
									il = i2;
									ip = i3;
								}
								else
								{
									xl = (x3 * (yy - y2) - x2 * (yy - y3)) / dy;
									xp = (x3 * (yy - y2) - xs * (yy - y3)) / dy;
									il = (i3 * (yy - y2) - i2 * (yy - y3)) / dy;
									ip = (i3 * (yy - y2) - isd * (yy - y3)) / dy;
								}

								if (xl > xp)
								{
									tmp = xl;
									xl = xp;
									xp = tmp;
									tmp = il;
									il = ip;
									ip = tmp;
								}

								dx = (int)(xl - xp);


								for (xx = xl; xx <= xp; xx++)
								{// podho po riadku !

									if ((dx == 0))
										ind = il;
									else
										ind = (il * (xx - xp) - ip * (xx - xl)) / dx;

									db.SetPixel((int)xx, (int)yy, Color.FromArgb(255, (int)ind, 0, 0));

									//GDI.SetPixel(hdc, xx, yy, rgbColor((uchar)(ind), 0, 0));
									//putPixel(xx,yy,rgbColor((uchar)(ind),0,0));
								}
							}
						}
						// tu konci Gouraudovanie 1 plochy
					}
					else // ostra !!!
					{

						long x1, x2, x3, y1, y2, y3;
						long xs;
						long xl, xp;
						long xx, yy, ind;
						long tmp;


						x1 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod1].x + 0.25 * body[plochy[uspplochy[j]].bod1].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
						y1 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod1].y + 0.25 * body[plochy[uspplochy[j]].bod1].z) + yres / 2 + 100 / kamera.zk * kamera.yk);
						x2 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod2].x + 0.25 * body[plochy[uspplochy[j]].bod2].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
						y2 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod2].y + 0.25 * body[plochy[uspplochy[j]].bod2].z) + yres / 2 + 100 / kamera.zk * kamera.yk);
						x3 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod3].x + 0.25 * body[plochy[uspplochy[j]].bod3].z) + xres / 2 + 100 / kamera.zk * kamera.xk);
						y3 = (long)(100 / kamera.zk * (body[plochy[uspplochy[j]].bod3].y + 0.25 * body[plochy[uspplochy[j]].bod3].z) + yres / 2 + 100 / kamera.zk * kamera.yk);

						// tu zacina vyplnanie 1 plochy
						ind = (long)(255 * plochy[uspplochy[j]].norz);

						if (y1 > y2)
						{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; }
						if (y2 > y3)
						{ tmp = y2; y2 = y3; y3 = tmp; tmp = x2; x2 = x3; x3 = tmp; }
						if (y1 > y2)
						{ tmp = y1; y1 = y2; y2 = tmp; tmp = x1; x1 = x2; x2 = tmp; }
						// toto bolo usporaduvanie podla y suradnice.

						if (Math.Abs(y3 - y1) > 1.0)
						{
							// najdenie pomocneho bodu
							xs = (x1 * (y2 - y3) - x3 * (y2 - y1)) / (y1 - y3);

							dy = (int)(y1 - y2);

							for (yy = y1; yy <= y2; yy++)
							{ // horny trojuholnik

								if ((dy == 0))
								{
									xl = x1;
									xp = x2;
								}
								else
								{
									xl = (x1 * (yy - y2) - x2 * (yy - y1)) / dy;
									xp = (x1 * (yy - y2) - xs * (yy - y1)) / dy;
								}

								pen.Color = Color.FromArgb(255, (int)ind, 0, 0);
								g.DrawLine(pen, xl, yy, xp, yy);

								//Graphics.DrawLine(pen, xl, yy, xp, yy);
								//line(xl, yy, xp, yy, rgbColor((uchar)(ind), 0, 0));


							}
							dy = (int)(y3 - y2);

							for (yy = y3; yy >= y2; yy--)
							{ // dolny trojuholnik

								if ((dy == 0))
								{
									xl = x2;
									xp = x3;
								}
								else
								{
									xl = (x3 * (yy - y2) - x2 * (yy - y3)) / dy;
									xp = (x3 * (yy - y2) - xs * (yy - y3)) / dy;
								}


								db.SetPixel((int)xl, (int)yy, Color.FromArgb(255, (int)ind, 0, 0));
								g.DrawLine(pen, xl, yy, xp, yy);
								//pen.Color = Color.FromArgb(255, (int)ind, 0, 0);
								//Graphics.DrawLine(pen, xl, yy, xp, yy);
								//line(xl,yy,xp,yy,rgbColor((uchar)(ind),0,0));

							}
						}
					}
				}
			}
/*
		klav:

			do

			{
				//asm {				// snimaj kb
				//	in al,60h
				//	mov a,al
				//	in al,61h
				//	mov ah,al
				//	or al,80h
				//	out 61h,al
				//	xchg ah,al
				//	out 61h,al
				//	mov al,20h
				//	out 20h,al
				//	mov cx,0x0000    // toto je finta proti
				//	mov dx,0x041c    // pretekaniu kb. bufra
				//	mov es,cx
				//	mov di,dx
				//	mov ax,es:[di]
				//	mov dx,0x041a
				//	mov di,dx
				//	mov es:[di],ax  // az potadeto
				//}

			}

			while (a > 100);

			switch (a)
			{

				case 80:
					for (int j = 0; j < pocetbodov; j++)
					{             //rotacia bodov hran okolo x
						z1 = body[j].z * 0.995 - body[j].y * 0.09983;
						y1 = body[j].z * 0.09983 + body[j].y * 0.995;
						body[j].z = z1;
						body[j].y = y1;
						z1 = body[j].norz * 0.995 - body[j].nory * 0.09983;
						y1 = body[j].norz * 0.09983 + body[j].nory * 0.995;
						body[j].norz = z1;
						body[j].nory = y1;
					}

					for (int j = 0; j < pocetploch; j++)
					{
						z1 = plochy[j].norz * 0.995 - plochy[j].nory * 0.09983;
						y1 = plochy[j].norz * 0.09983 + plochy[j].nory * 0.995;
						plochy[j].norz = z1;
						plochy[j].nory = y1;
					}


				;
					break;

				case 72:
					for (int j = 0; j < pocetbodov; j++)
					{             //rotacia bodov hran okolo x
						z1 = body[j].z * 0.995 - body[j].y * -0.09983;
						y1 = body[j].z * -0.09983 + body[j].y * 0.995;
						body[j].z = z1;
						body[j].y = y1;
						z1 = body[j].norz * 0.995 - body[j].nory * -0.09983;
						y1 = body[j].norz * -0.09983 + body[j].nory * 0.995;
						body[j].norz = z1;
						body[j].nory = y1;

					}

					for (int j = 0; j < pocetploch; j++)
					{
						z1 = plochy[j].norz * 0.995 - plochy[j].nory * -0.09983;
						y1 = plochy[j].norz * -0.09983 + plochy[j].nory * 0.995;
						plochy[j].norz = z1;
						plochy[j].nory = y1;
					}




				;
					break;
				case 71:
					for (int j = 0; j < pocetbodov; j++)
					{             //rotacia bodov hran okolo y
						x1 = body[j].x * 0.995 - body[j].z * 0.09983;
						z1 = body[j].x * 0.09983 + body[j].z * 0.995;
						body[j].x = x1;
						body[j].z = z1;
						x1 = body[j].norx * 0.995 - body[j].norz * 0.09983;
						z1 = body[j].norx * 0.09983 + body[j].norz * 0.995;
						body[j].norx = x1;
						body[j].norz = z1;

					}

					for (int j = 0; j < pocetploch; j++)
					{
						x1 = plochy[j].norx * 0.995 - plochy[j].norz * 0.09983;
						z1 = plochy[j].norx * 0.09983 + plochy[j].norz * 0.995;
						plochy[j].norx = x1;
						plochy[j].norz = z1;
					}



				;
					break;
				case 73:
					for (int j = 0; j < pocetbodov; j++)
					{             //rotacia bodov hran okolo y
						x1 = body[j].x * 0.995 - body[j].z * -0.09983;
						z1 = body[j].x * -0.09983 + body[j].z * 0.995;
						body[j].x = x1;
						body[j].z = z1;
						x1 = body[j].norx * 0.995 - body[j].norz * -0.09983;
						z1 = body[j].norx * -0.09983 + body[j].norz * 0.995;
						body[j].norx = x1;
						body[j].norz = z1;

					}

					for (int j = 0; j < pocetploch; j++)
					{
						x1 = plochy[j].norx * 0.995 - plochy[j].norz * -0.09983;
						z1 = plochy[j].norx * -0.09983 + plochy[j].norz * 0.995;
						plochy[j].norx = x1;
						plochy[j].norz = z1;
					}


				;
					break;
				case 77:
					for (int j = 0; j < pocetbodov; j++)
					{             //rotacia bodov hran okolo z
						x1 = body[j].x * 0.995 - body[j].y * 0.09983;
						y1 = body[j].x * 0.09983 + body[j].y * 0.995;
						body[j].x = x1;
						body[j].y = y1;
						x1 = body[j].norx * 0.995 - body[j].nory * 0.09983;
						y1 = body[j].norx * 0.09983 + body[j].nory * 0.995;
						body[j].norx = x1;
						body[j].nory = y1;

					}
					for (int j = 0; j < pocetploch; j++)
					{
						x1 = plochy[j].norx * 0.995 - plochy[j].nory * 0.09983;
						y1 = plochy[j].norx * 0.09983 + plochy[j].nory * 0.995;
						plochy[j].norx = x1;
						plochy[j].nory = y1;
					}

				;
					break;

				case 75:
					for (int j = 0; j < pocetbodov; j++)
					{             //rotacia bodov hran okolo z
						x1 = body[j].x * 0.995 - body[j].y * -0.09983;
						y1 = body[j].x * -0.09983 + body[j].y * 0.995;
						body[j].x = x1;
						body[j].y = y1;
						x1 = body[j].norx * 0.995 - body[j].nory * -0.09983;
						y1 = body[j].norx * -0.09983 + body[j].nory * 0.995;
						body[j].norx = x1;
						body[j].nory = y1;

					}

					for (int j = 0; j < pocetploch; j++)
					{
						x1 = plochy[j].norx * 0.995 - plochy[j].nory * -0.09983;
						y1 = plochy[j].norx * -0.09983 + plochy[j].nory * 0.995;
						plochy[j].norx = x1;
						plochy[j].nory = y1;
					}



				;
					break;
				case 30:
					kamera.xk -= 0.2;
					break;
				case 31:
					kamera.yk += 0.2;
					break;
				case 32:
					kamera.xk += 0.2;
					break;
				case 17:
					kamera.yk -= 0.2;
					break;
				case 33:
					if (kamera.zk > 0.3)
						kamera.zk -= 0.2;
					break;
				case 34:
					kamera.zk += 0.2;
					break;
				case 1: //restoreMode();
					goto zac;
					break;
				default:
					goto klav;
					break;
			}

		tuto:
			//vpage = ++vpage % (maxpage + 1);
			///////////////////////////////////////////////setVisualPage(vpage);
			//apage = ++apage % (maxpage + 1);
*/
			picture.Image = db.Bitmap;

			//
			//goto zaciatok;

		}

	}
}
