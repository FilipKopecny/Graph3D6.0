#include <conio.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <iostream.h>
#include <fstream.h>
#include "pmode.h"
#include "svga.h"
#include "deklar.h"



main()
{
char prvok[20];
ifstream vstup;
int mbs,mbe,k,t,xo1,yo1,xo2,yo2,p,ii;
mbs=mbe=k=t=0;
char subor[24];
float x1,y1,z1;
//int kk=0;


if ((body = (bod *) malloc(1300*sizeof(bod))) == NULL)
{
   printf("Not enough memory to allocate points\n");
   exit(1);  /* terminate program if out of memory */
}

if ((hrany = (hrana *) malloc(2600*sizeof(hrana))) == NULL)
{
   printf("Not enough memory to allocate edges\n");
   exit(1);  /* terminate program if out of memory */
}

if ((plochy = (plocha *) malloc(1300*sizeof(plocha))) == NULL)
{
   printf("Not enough memory to allocate sides\n");
   exit(1);  /* terminate program if out of memory */
}

clrscr();
puts("zadaj subor");
gets(subor);

vstup.open(subor);
if (vstup.good()) {

	while (vstup.good()) {
		vstup >> prvok; 		// nacitaj meno objektu

		if (!stricmp(prvok,"kamera")) {
			vstup >> kamery.xk >> kamery.yk >> kamery.zk >> kamery.uhol;
		}
		else


		if (!stricmp(prvok,"kvader")) {	// tu sa budu citat params pre kvader
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

		if (!stricmp(prvok,"toroid")) {	// tu sa budu citat params pre toroid
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

			if (!stricmp(prvok,"meshbs")) {	// tu sa budu citat params pre toroid
			vstup >> meshebs[mbs].x;
			vstup >> meshebs[mbs].y;
			vstup >> meshebs[mbs].z;
			vstup >> meshebs[mbs].xuhol;
			vstup >> meshebs[mbs].yuhol;
			vstup >> meshebs[mbs].zuhol;
			for (int k=0;k<5;k++)
			for (int l=0;l<5;l++)
			{
				vstup >> meshebs[mbs].b[l][k].x;
				vstup >> meshebs[mbs].b[l][k].y;
				vstup >> meshebs[mbs].b[l][k].z;
			}
			mbs++;
		}

		else
			if (!stricmp(prvok,"meshbe")) {	// tu sa budu citat params pre toroid
			vstup >> meshebe[mbe].x;
			vstup >> meshebe[mbe].y;
			vstup >> meshebe[mbe].z;
			vstup >> meshebe[mbe].xuhol;
			vstup >> meshebe[mbe].yuhol;
			vstup >> meshebe[mbe].zuhol;
			for (int k=0;k<7;k++)
			for (int l=0;l<7;l++)
			{
				vstup >> meshebe[mbe].b[l][k].x;
				vstup >> meshebe[mbe].b[l][k].y;
				vstup >> meshebe[mbe].b[l][k].z;
			}
			mbe++;
		}
		else

		if (!stricmp(prvok,"koniec")) {
			vstup.close();
			break;
		}

		else {
			puts ("Chyba ! Neznamy prvok !");
			vstup.close();
			exit(0);
		}
	}
}

clrscr();
printf ("Nacitalo sa %d kvadrov, %d toroidov a %d ploch spline a %d ploch bezier \n",k,t,mbs,mbe);
getchar();

int i,j;

for (i=0;i<k;i++) {             //vyroba hran kociek
int ptemp=pocetbodov;

	double px=kvadre[i].dx/2;
	double py=kvadre[i].dy/2;
	double pz=kvadre[i].dz/2;

	body[pocetbodov].x	=px;
	body[pocetbodov].y	=py;
	body[pocetbodov++].z=pz;

	body[pocetbodov].x	=-px;
	body[pocetbodov].y	=py;
	body[pocetbodov++].z=pz;

	body[pocetbodov].x	=-px;
	body[pocetbodov].y	=py;
	body[pocetbodov++].z=-pz;

	body[pocetbodov].x	=px;
	body[pocetbodov].y	=py;
	body[pocetbodov++].z=-pz;

	body[pocetbodov].x	=px;
	body[pocetbodov].y	=-py;
	body[pocetbodov++].z=pz;

	body[pocetbodov].x	=-px;
	body[pocetbodov].y	=-py;
	body[pocetbodov++].z=pz;

	body[pocetbodov].x	=-px;
	body[pocetbodov].y	=-py;
	body[pocetbodov++].z=-pz;

	body[pocetbodov].x	=px;
	body[pocetbodov].y	=-py;
	body[pocetbodov++].z=-pz;


	for (j=pocetbodov-8;j<pocetbodov;j++) {             //rotacia bodov okolo z
		x1 = body[j].x*cos(kvadre[i].zuhol)-body[j].y*sin(kvadre[i].zuhol);
		y1 = body[j].x*sin(kvadre[i].zuhol)+body[j].y*cos(kvadre[i].zuhol);
		body[j].x=x1;
		body[j].y=y1;
	}

	for (j=pocetbodov-8;j<pocetbodov;j++) {             //rotacia bodov okolo y
		x1 = body[j].x*cos(kvadre[i].yuhol)-body[j].z*sin(kvadre[i].yuhol);
		z1 = body[j].x*sin(kvadre[i].yuhol)+body[j].z*cos(kvadre[i].yuhol);
		body[j].x=x1;
		body[j].z=z1;
	}

	for (j=pocetbodov-8;j<pocetbodov;j++) {             //rotacia bodov hran okolo z
		z1 = body[j].z*cos(kvadre[i].xuhol)-body[j].y*sin(kvadre[i].xuhol);
		y1 = body[j].z*sin(kvadre[i].xuhol)+body[j].y*cos(kvadre[i].xuhol);
		body[j].z=z1;
		body[j].y=y1;

	}

	for (j=pocetbodov-8;j<pocetbodov;j++) {             //rotacia bodov hran okolo z
		body[j].x += kvadre[i].x;
		body[j].y += kvadre[i].y;
		body[j].z += kvadre[i].z;
	}

	for (j=0;j<4;j++)	{
			hrany[pocethran].bod1 = ptemp + j;
			hrany[pocethran++].bod2 = ptemp + j + 4;
			hrany[pocethran].bod1 = ptemp + j;
			if (j!=3) hrany[pocethran++].bod2 = ptemp + j + 5;
			else hrany[pocethran++].bod2 = ptemp + j + 1;
	}
	 for (j=0;j<3;j++)	{
			hrany[pocethran].bod1 = ptemp + j;
			hrany[pocethran++].bod2 = ptemp + j + 1;
	}
			hrany[pocethran].bod1 = ptemp + 3;
			hrany[pocethran++].bod2 = ptemp;
	 for (j=0;j<4;j++)	{
			hrany[pocethran].bod1 = ptemp + j + 4;
			if (j!=3) hrany[pocethran++].bod2 = ptemp + j + 5;
			else hrany[pocethran++].bod2 = ptemp + j + 1;
	}

			hrany[pocethran].bod1 = ptemp;
			hrany[pocethran++].bod2 = ptemp + 2;
			hrany[pocethran].bod1 = ptemp + 5;
			hrany[pocethran++].bod2 = ptemp + 7;


	for (j=0;j<3;j++) {
		plochy[pocetploch].bod1 = ptemp + j;
		plochy[pocetploch].bod2 = ptemp + j + 1;
		plochy[pocetploch].bod3 = ptemp + j + 5;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);
	}

		plochy[pocetploch].bod3 = ptemp + 3;
		plochy[pocetploch].bod2 = ptemp + 3 + 1;
		plochy[pocetploch].bod1 = ptemp;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);



	for (j=4;j<7;j++) {
		plochy[pocetploch].bod2 = ptemp + j;
		plochy[pocetploch].bod1 = ptemp + j + 1;
		plochy[pocetploch].bod3 = ptemp + j - 4;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);
	}
		plochy[pocetploch].bod2 = ptemp + 7;
		plochy[pocetploch].bod1 = ptemp + 4;
		plochy[pocetploch].bod3 = ptemp + 3;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);

		plochy[pocetploch].bod2 = ptemp;
		plochy[pocetploch].bod1 = ptemp + 1;
		plochy[pocetploch].bod3 = ptemp + 2;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);

		plochy[pocetploch].bod2 = ptemp;
		plochy[pocetploch].bod1 = ptemp + 2;
		plochy[pocetploch].bod3 = ptemp + 3;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);

		plochy[pocetploch].bod1 = 4+ ptemp;
		plochy[pocetploch].bod2 = 4+ptemp + 1;
		plochy[pocetploch].bod3 = 4+ptemp + 2;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);

		plochy[pocetploch].bod1 = 4+ptemp;
		plochy[pocetploch].bod2 = 4+ptemp + 2;
		plochy[pocetploch].bod3 = 4+ptemp + 3;
		plochy[pocetploch].zaobli=0;
		normalizuj(pocetploch++);

}


for (i=0;i<t;i++) {             //vyroba hran toroidov
	int ptemp=pocetbodov;
	FillTorus(toroidy[i].rin,toroidy[i].xjemnost,toroidy[i].rout,toroidy[i].yjemnost);



	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov okolo z
		x1 = body[j].x*cos(toroidy[i].zuhol)-body[j].y*sin(toroidy[i].zuhol);
		y1 = body[j].x*sin(toroidy[i].zuhol)+body[j].y*cos(toroidy[i].zuhol);
		body[j].x=x1;
		body[j].y=y1;
	}

	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov okolo y
		x1 = body[j].x*cos(toroidy[i].yuhol)-body[j].z*sin(toroidy[i].yuhol);
		z1 = body[j].x*sin(toroidy[i].yuhol)+body[j].z*cos(toroidy[i].yuhol);
		body[j].x=x1;
		body[j].z=z1;
	}

	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov hran okolo z
		z1 = body[j].z*cos(toroidy[i].xuhol)-body[j].y*sin(toroidy[i].xuhol);
		y1 = body[j].z*sin(toroidy[i].xuhol)+body[j].y*cos(toroidy[i].xuhol);
		body[j].z=z1;
		body[j].y=y1;

	}


	for (j=ptemp;j<pocetbodov;j++) {             //posun bodov hran
		body[j].x += toroidy[i].x;
		body[j].y += toroidy[i].y;
		body[j].z += toroidy[i].z;
	}


int xj=toroidy[i].xjemnost;
int yj=toroidy[i].yjemnost;


	for (j=0;j<xj;j++)	{
		for (ii=0;ii<yj;ii++)	{
			hrany[pocethran].bod1= ii%yj   + (j%xj)*yj 		+ptemp;
			hrany[pocethran++].bod2= ii%yj + ((j+1)%xj)*yj	+ptemp;
		}
	}


	for (j=0;j<xj;j++)	{
		for (ii=0;ii<yj;ii++)	{
			hrany[pocethran].bod1= ii%yj 	   + (j%xj)*yj	+ptemp;
			hrany[pocethran++].bod2= (ii+1)%yj + yj*(j%xj)	+ptemp;
		}
	}

	for (j=0;j<xj;j++)	{
		for (ii=0;ii<yj;ii++)	{
			hrany[pocethran].bod1= ii%yj 	   + yj*((j+1)%xj)	+ptemp;
			hrany[pocethran++].bod2= (ii+1)%yj + (j%xj)*yj		+ptemp;
		}
	}



	for (j=0;j<xj;j++)	{
		for (ii=0;ii<yj;ii++)	{
			 plochy[pocetploch].bod1 = ptemp +  ii%yj 		+ (j%xj)*yj;
			 plochy[pocetploch].bod2 = ptemp +  (ii+1)%yj 	+ (j%xj)*yj;
			 plochy[pocetploch].bod3 = ptemp +  ii%yj 		+ ((j+1)%xj)*yj;
			plochy[pocetploch].zaobli=1;
			normalizuj(pocetploch++);
		}
	 }

	for (j=1;j<=xj;j++)	{
		for (ii=1;ii<=yj;ii++)	{
			 plochy[pocetploch].bod1 = ptemp +  ii%yj + (j%xj)*yj;
			 plochy[pocetploch].bod2 = ptemp +  (ii-1)%yj + (j%xj)*yj;
			 plochy[pocetploch].bod3 = ptemp +  ii%yj + ((j-1)%xj)*yj;
			plochy[pocetploch].zaobli=1;
			normalizuj(pocetploch++);
		}
	 }



}



for (i=0;i<mbs;i++) {             //vyroba hran meshov

	int ptemp=pocetbodov;

	Bspline(i);


	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov okolo z
		x1 = body[j].x*cos(meshebs[i].zuhol)-body[j].y*sin(meshebs[i].zuhol);
		y1 = body[j].x*sin(meshebs[i].zuhol)+body[j].y*cos(meshebs[i].zuhol);
		body[j].x=x1;
		body[j].y=y1;
	}

	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov okolo y
		x1 = body[j].x*cos(meshebs[i].yuhol)-body[j].z*sin(meshebs[i].yuhol);
		z1 = body[j].x*sin(meshebs[i].yuhol)+body[j].z*cos(meshebs[i].yuhol);
		body[j].x=x1;
		body[j].z=z1;
	}

	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov hran okolo z
		z1 = body[j].z*cos(meshebs[i].xuhol)-body[j].y*sin(meshebs[i].xuhol);
		y1 = body[j].z*sin(meshebs[i].xuhol)+body[j].y*cos(meshebs[i].xuhol);
		body[j].z=z1;
		body[j].y=y1;

	}


	for (j=ptemp;j<pocetbodov;j++) {             //posun bodov hran
		body[j].x += meshebs[i].x;
		body[j].y += meshebs[i].y;
		body[j].z += meshebs[i].z;
	}



	for (j=0;j<11;j++)	{
		for (ii=0;ii<5;ii++)	{

			hrany[pocethran].bod1 = ptemp + ii + j*6;
			hrany[pocethran++].bod2 = ptemp + ii+1 + j*6;

			if (j!=10)	{
				hrany[pocethran].bod1 = ptemp + ii + j*6;
				hrany[pocethran++].bod2 = ptemp + ii + (j+1)*6;

				hrany[pocethran].bod1 = ptemp + ii+1 + j*6;
				hrany[pocethran++].bod2 = ptemp + ii + (j+1)*6;
				}
			}
		}

	for (j=0;j<11;j++)	{
		for (ii=0;ii<5;ii++)	{
			if (ii==0) {
				hrany[pocethran].bod1 = ptemp +  5  + j*6;
				hrany[pocethran++].bod2 = ptemp +  65 + ii+1 + j*5;
				if (j!=10)	{
					hrany[pocethran].bod1 = ptemp +  65 + ii+1 + j*5;
					hrany[pocethran++].bod2 = ptemp +  5  + (j+1)*6;

					hrany[pocethran].bod1 = ptemp +  5  + j*6;
					hrany[pocethran++].bod2 = ptemp +  5  + (j+1)*6;
				}

			}
			else	{

				hrany[pocethran].bod1 = ptemp + 65 + ii + j*5;
				hrany[pocethran++].bod2 = ptemp + 65 + ii+1 + j*5;

				if (j!=10)	{
					hrany[pocethran].bod1 = ptemp + 65 + ii+1 + j*5;
					hrany[pocethran++].bod2 = ptemp + 65 + ii + (j+1)*5;

					hrany[pocethran].bod1 = ptemp + 65 + ii + j*5;
					hrany[pocethran++].bod2 = ptemp + 65 + ii + (j+1)*5;
				}
			}
		}
	}


	for (j=0;j<10;j++)	{
		hrany[pocethran].bod1 = ptemp + 65 + 5 + j*5;
		hrany[pocethran++].bod2 = ptemp + 65 + 5 + (j+1)*5;
	}





	for (j=0;j<10;j++)	{
		for (ii=0;ii<5;ii++)	{
			plochy[pocetploch].bod1 = ptemp + ii + j*6;
			plochy[pocetploch].bod2 = ptemp + ii+1 + j*6;
			plochy[pocetploch].bod3 = ptemp + ii + (j+1)*6;
			plochy[pocetploch].zaobli=1;
			normalizuj(pocetploch++);
			}
		}
	for (j=1;j<11;j++)	{
		for (ii=1;ii<6;ii++)	{
			 plochy[pocetploch].bod1 = ptemp + ii + j*6;
			 plochy[pocetploch].bod2 = ptemp + ii-1 + j*6;
			 plochy[pocetploch].bod3 = ptemp + ii + (j-1)*6;
			plochy[pocetploch].zaobli=1;
			normalizuj(pocetploch++);
		}
	 }


	for (j=0;j<10;j++)	{
		for (ii=0;ii<5;ii++)	{
			if (ii==0) {
				plochy[pocetploch].bod1 = ptemp +  5  + j*6;
				plochy[pocetploch].bod2 = ptemp +  65 + ii+1 + j*5;
				plochy[pocetploch].bod3 = ptemp +  5  + (j+1)*6;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
			else	{

				plochy[pocetploch].bod1 = ptemp + 65 + ii + j*5;
				plochy[pocetploch].bod2 = ptemp + 65 + ii+1 + j*5;
				plochy[pocetploch].bod3 = ptemp + 65 + ii + (j+1)*5;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
		}
	}

	for (j=1;j<11;j++)	{
		for (ii=1;ii<6;ii++)	{
			if (ii==1) {
				plochy[pocetploch].bod1 = ptemp +  65 + ii  + j*5;
				plochy[pocetploch].bod2 = ptemp +  5 + ii-1 + j*6;
				plochy[pocetploch].bod3 = ptemp +  65 + ii + (j-1)*5;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
			else	{

				plochy[pocetploch].bod1 = ptemp + 65 + ii + j*5;
				plochy[pocetploch].bod2 = ptemp + 65 + ii-1 + j*5;
				plochy[pocetploch].bod3 = ptemp + 65 + ii + (j-1)*5;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
		}
	}






}

for (i=0;i<mbe;i++) {

	int ptemp=pocetbodov;

	Bezier(i);




	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov okolo z
		x1 = body[j].x*cos(meshebe[i].zuhol)-body[j].y*sin(meshebe[i].zuhol);
		y1 = body[j].x*sin(meshebe[i].zuhol)+body[j].y*cos(meshebe[i].zuhol);
		body[j].x=x1;
		body[j].y=y1;
	}

	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov okolo y
		x1 = body[j].x*cos(meshebe[i].yuhol)-body[j].z*sin(meshebe[i].yuhol);
		z1 = body[j].x*sin(meshebe[i].yuhol)+body[j].z*cos(meshebe[i].yuhol);
		body[j].x=x1;
		body[j].z=z1;
	}

	for (j=ptemp;j<pocetbodov;j++) {             //rotacia bodov hran okolo z
		z1 = body[j].z*cos(meshebe[i].xuhol)-body[j].y*sin(meshebe[i].xuhol);
		y1 = body[j].z*sin(meshebe[i].xuhol)+body[j].y*cos(meshebe[i].xuhol);
		body[j].z=z1;
		body[j].y=y1;

	}


	for (j=ptemp;j<pocetbodov;j++) {             //posun bodov hran
		body[j].x += meshebe[i].x;
		body[j].y += meshebe[i].y;
		body[j].z += meshebe[i].z;
	}


	for (j=0;j<11;j++)	{
		for (ii=0;ii<5;ii++)	{

			hrany[pocethran].bod1 = ptemp + ii + j*6;
			hrany[pocethran++].bod2 = ptemp + ii+1 + j*6;

			if (j!=10)	{
				hrany[pocethran].bod1 = ptemp + ii + j*6;
				hrany[pocethran++].bod2 = ptemp + ii + (j+1)*6;

				hrany[pocethran].bod1 = ptemp + ii+1 + j*6;
				hrany[pocethran++].bod2 = ptemp + ii + (j+1)*6;
				}
			}
		}

	for (j=0;j<11;j++)	{
		for (ii=0;ii<5;ii++)	{
			if (ii==0) {
				hrany[pocethran].bod1 = ptemp +  5  + j*6;
				hrany[pocethran++].bod2 = ptemp +  65 + ii+1 + j*5;
				if (j!=10)	{
					hrany[pocethran].bod1 = ptemp +  65 + ii+1 + j*5;
					hrany[pocethran++].bod2 = ptemp +  5  + (j+1)*6;

					hrany[pocethran].bod1 = ptemp +  5  + j*6;
					hrany[pocethran++].bod2 = ptemp +  5  + (j+1)*6;
				}

			}
			else	{

				hrany[pocethran].bod1 = ptemp + 65 + ii + j*5;
				hrany[pocethran++].bod2 = ptemp + 65 + ii+1 + j*5;

				if (j!=10)	{
					hrany[pocethran].bod1 = ptemp + 65 + ii+1 + j*5;
					hrany[pocethran++].bod2 = ptemp + 65 + ii + (j+1)*5;

					hrany[pocethran].bod1 = ptemp + 65 + ii + j*5;
					hrany[pocethran++].bod2 = ptemp + 65 + ii + (j+1)*5;
				}
			}
		}
	}


	for (j=0;j<10;j++)	{
		hrany[pocethran].bod1 = ptemp + 65 + 5 + j*5;
		hrany[pocethran++].bod2 = ptemp + 65 + 5 + (j+1)*5;
	}





	for (j=0;j<10;j++)	{
		for (ii=0;ii<5;ii++)	{
			plochy[pocetploch].bod1 = ptemp + ii + j*6;
			plochy[pocetploch].bod2 = ptemp + ii+1 + j*6;
			plochy[pocetploch].bod3 = ptemp + ii + (j+1)*6;
			plochy[pocetploch].zaobli=1;
			normalizuj(pocetploch++);
			}
		}
	for (j=1;j<11;j++)	{
		for (ii=1;ii<6;ii++)	{
			 plochy[pocetploch].bod1 = ptemp + ii + j*6;
			 plochy[pocetploch].bod2 = ptemp + ii-1 + j*6;
			 plochy[pocetploch].bod3 = ptemp + ii + (j-1)*6;
			plochy[pocetploch].zaobli=1;
			normalizuj(pocetploch++);
		}
	 }


	for (j=0;j<10;j++)	{
		for (ii=0;ii<5;ii++)	{
			if (ii==0) {
				plochy[pocetploch].bod1 = ptemp +  5  + j*6;
				plochy[pocetploch].bod2 = ptemp +  65 + ii+1 + j*5;
				plochy[pocetploch].bod3 = ptemp +  5  + (j+1)*6;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
			else	{

				plochy[pocetploch].bod1 = ptemp + 65 + ii + j*5;
				plochy[pocetploch].bod2 = ptemp + 65 + ii+1 + j*5;
				plochy[pocetploch].bod3 = ptemp + 65 + ii + (j+1)*5;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
		}
	}

	for (j=1;j<11;j++)	{
		for (ii=1;ii<6;ii++)	{
			if (ii==1) {
				plochy[pocetploch].bod1 = ptemp +  65 + ii  + j*5;
				plochy[pocetploch].bod2 = ptemp +  5 + ii-1 + j*6;
				plochy[pocetploch].bod3 = ptemp +  65 + ii + (j-1)*5;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
			else	{

				plochy[pocetploch].bod1 = ptemp + 65 + ii + j*5;
				plochy[pocetploch].bod2 = ptemp + 65 + ii-1 + j*5;
				plochy[pocetploch].bod3 = ptemp + 65 + ii + (j-1)*5;
				plochy[pocetploch].zaobli=1;
				normalizuj(pocetploch++);
			}
		}
	}



}


//*************************** Tu sa zacina zobrazovanie *************

clrscr();
   int x, y;
   char ch;
   char a;

zac:fflush(stdin);

	printf("bodov : %d\nhran : %d\nploch : %d\n\n",pocetbodov,pocethran,pocetploch);

	puts("Toto je menu. Vyber si ...");
	puts("1...body");
	puts("2...vsetky hrany");
	puts("3...privratene plochy konstantne tienovane");
	puts("4...(3) plus maliarov algoritmus");
	puts("5...Privratene plochy maliarov algoritmus Gouraudovo tienovanie");
	puts("6...koniec");

	ch=getch ();
//    ch-='0';

	if (ch!='1')if(ch!='2')if(ch!='3')if(ch!='4')if(ch!='5') exit(0);

initSuperVGA(true);

if(!testDirectColor(0x7FFFL))
	exit(1);

 if (maxpage != 0) {
		vpage = 1;
		apage = 0;
		setActivePage(apage);
		setVisualPage(apage);
	}

zaciatok:
		   setActivePage(apage);
		   clear(0);

 if (ch=='1')
	for (j=0;j<pocetbodov;j++) 	{   // chod cez fsetky plochy

		xo1=100/kamery.zk*(body[j].x+0.25*body[j].z)+xres/2+100/kamery.zk*kamery.xk;
		yo1=100/kamery.zk*(body[j].y+0.25*body[j].z)+yres/2+100/kamery.zk*kamery.yk;
		putPixel(xo1,yo1,rgbColor((uchar)(255),0,0));
	}

 if (ch=='2')
	for (j=0;j<pocethran;j++) 	{   // chod cez fsetky plochy
		xo1=100/kamery.zk*(body[hrany[j].bod1].x+0.25*body[hrany[j].bod1].z)+xres/2+100/kamery.zk*kamery.xk;
		yo1=100/kamery.zk*(body[hrany[j].bod1].y+0.25*body[hrany[j].bod1].z)+yres/2+100/kamery.zk*kamery.yk;
		xo2=100/kamery.zk*(body[hrany[j].bod2].x+0.25*body[hrany[j].bod2].z)+xres/2+100/kamery.zk*kamery.xk;
		yo2=100/kamery.zk*(body[hrany[j].bod2].y+0.25*body[hrany[j].bod2].z)+yres/2+100/kamery.zk*kamery.yk;
		line(xo1,yo1,xo2,yo2,rgbColor((uchar)(255),0,0));
	}

 if (ch=='3')
	for (j=0;j<pocetploch;j++) 	{   // chod cez fsetky plochy
		if (plochy[j].norz>0)	{


		float x1,x2,x3,y1,y2,y3;
		float xs;
		float xl,xp;
		int xx,yy,in;
		float tmp;


		x1=100/kamery.zk*(body[plochy[j].bod1].x+0.25*body[plochy[j].bod1].z)+xres/2+100/kamery.zk*kamery.xk;
		y1=100/kamery.zk*(body[plochy[j].bod1].y+0.25*body[plochy[j].bod1].z)+yres/2+100/kamery.zk*kamery.yk;
		x2=100/kamery.zk*(body[plochy[j].bod2].x+0.25*body[plochy[j].bod2].z)+xres/2+100/kamery.zk*kamery.xk;
		y2=100/kamery.zk*(body[plochy[j].bod2].y+0.25*body[plochy[j].bod2].z)+yres/2+100/kamery.zk*kamery.yk;
		x3=100/kamery.zk*(body[plochy[j].bod3].x+0.25*body[plochy[j].bod3].z)+xres/2+100/kamery.zk*kamery.xk;
		y3=100/kamery.zk*(body[plochy[j].bod3].y+0.25*body[plochy[j].bod3].z)+yres/2+100/kamery.zk*kamery.yk;

			// tu zacina vyplnanie 1 plochy
		in=255*plochy[j].norz;


		if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;}
		if (y2>y3) {tmp=y2;y2=y3;y3=tmp;tmp=x2;x2=x3;x3=tmp;}
		if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;}
			// toto bolo usporaduvanie podla y suradnice.


		if (abs(y3-y1)>1.0)	{

				// najdenie pomocneho bodu
			xs = (x1*(y2-y3)-x3*(y2-y1))/(y1-y3);

			for (yy=y1;yy<=y2;yy++)	{ // horny trojuholnik

				if (abs(y1-y2)<1.0) {
					xl=x1;
					xp=x2;
				}
				else	{
					xl=(x1*(yy-y2)-x2*(yy-y1))/(y1-y2);
					xp=(x1*(yy-y2)-xs*(yy-y1))/(y1-y2);
				}

				line(xl,yy,xp,yy,rgbColor((uchar)(in),0,0));
			}


			for (yy=y3;yy>=y2;yy--)	{ // dolny trojuholnik

				if (abs(y3-y2)<1.0) {
					xl=x2;
					xp=x3;
				}
				else	{
					xl=(x3*(yy-y2)-x2*(yy-y3))/(y3-y2);
					xp=(x3*(yy-y2)-xs*(yy-y3))/(y3-y2);
				}

				line(xl,yy,xp,yy,rgbColor((uchar)(in),0,0));

			}

		}



		}
	}

 if (ch=='4') {
	Usporiadaj(); // <------- Maliars algorithmusz
	for (j=0;j<PocetUspPrivrPloch;j++) 	{   // chod cez fsetky plochy

		float x1,x2,x3,y1,y2,y3;
		float xs;
		float xl,xp;
		int xx,yy,in;
		float tmp;


		x1=100/kamery.zk*(body[plochy[uspplochy[j]].bod1].x+0.25*body[plochy[uspplochy[j]].bod1].z)+xres/2+100/kamery.zk*kamery.xk;
		y1=100/kamery.zk*(body[plochy[uspplochy[j]].bod1].y+0.25*body[plochy[uspplochy[j]].bod1].z)+yres/2+100/kamery.zk*kamery.yk;
		x2=100/kamery.zk*(body[plochy[uspplochy[j]].bod2].x+0.25*body[plochy[uspplochy[j]].bod2].z)+xres/2+100/kamery.zk*kamery.xk;
		y2=100/kamery.zk*(body[plochy[uspplochy[j]].bod2].y+0.25*body[plochy[uspplochy[j]].bod2].z)+yres/2+100/kamery.zk*kamery.yk;
		x3=100/kamery.zk*(body[plochy[uspplochy[j]].bod3].x+0.25*body[plochy[uspplochy[j]].bod3].z)+xres/2+100/kamery.zk*kamery.xk;
		y3=100/kamery.zk*(body[plochy[uspplochy[j]].bod3].y+0.25*body[plochy[uspplochy[j]].bod3].z)+yres/2+100/kamery.zk*kamery.yk;

			// tu zacina vyplnanie 1 plochy
		in=255*plochy[uspplochy[j]].norz;


		if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;}
		if (y2>y3) {tmp=y2;y2=y3;y3=tmp;tmp=x2;x2=x3;x3=tmp;}
		if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;}
			// toto bolo usporaduvanie podla y suradnice.


		if (abs(y3-y1)>1.0)	{

				// najdenie pomocneho bodu
			xs = (x1*(y2-y3)-x3*(y2-y1))/(y1-y3);

			for (yy=y1;yy<=y2;yy++)	{ // horny trojuholnik

				if (abs(y1-y2)<1.0) {
					xl=x1;
					xp=x2;
				}
				else	{
					xl=(x1*(yy-y2)-x2*(yy-y1))/(y1-y2);
					xp=(x1*(yy-y2)-xs*(yy-y1))/(y1-y2);
				}


				line(xl,yy,xp,yy,rgbColor((uchar)(in),0,0));


			}


			for (yy=y3;yy>=y2;yy--)	{ // dolny trojuholnik

				if (abs(y3-y2)<1.0) {
					xl=x2;
					xp=x3;
				}
				else	{
					xl=(x3*(yy-y2)-x2*(yy-y3))/(y3-y2);
					xp=(x3*(yy-y2)-xs*(yy-y3))/(y3-y2);
				}

				line(xl,yy,xp,yy,rgbColor((uchar)(in),0,0));

			}

		}

	}
}

 if (ch=='5') {

	Usporiadaj();
	dajnormalybodom();
	for (j=0;j<PocetUspPrivrPloch;j++) 	{   // chod cez fsetky plochy

	if (plochy[uspplochy[j]].zaobli) {


			float x1,x2,x3,y1,y2,y3;
			float xs;
			float xl,xp;
			float i1,i2,i3,is,il,ip,in;
			int xx,yy;
			float tmp;

			x1=100/kamery.zk*(body[plochy[uspplochy[j]].bod1].x+0.25*body[plochy[uspplochy[j]].bod1].z)+xres/2+100/kamery.zk*kamery.xk;
			y1=100/kamery.zk*(body[plochy[uspplochy[j]].bod1].y+0.25*body[plochy[uspplochy[j]].bod1].z)+yres/2+100/kamery.zk*kamery.yk;
			x2=100/kamery.zk*(body[plochy[uspplochy[j]].bod2].x+0.25*body[plochy[uspplochy[j]].bod2].z)+xres/2+100/kamery.zk*kamery.xk;
			y2=100/kamery.zk*(body[plochy[uspplochy[j]].bod2].y+0.25*body[plochy[uspplochy[j]].bod2].z)+yres/2+100/kamery.zk*kamery.yk;
			x3=100/kamery.zk*(body[plochy[uspplochy[j]].bod3].x+0.25*body[plochy[uspplochy[j]].bod3].z)+xres/2+100/kamery.zk*kamery.xk;
			y3=100/kamery.zk*(body[plochy[uspplochy[j]].bod3].y+0.25*body[plochy[uspplochy[j]].bod3].z)+yres/2+100/kamery.zk*kamery.yk;

			// tu zacina Gouraudovanie 1 plochy


			if (body[plochy[uspplochy[j]].bod1].norz>0)
				i1=body[plochy[uspplochy[j]].bod1].norz*255;
			else
				i1=0;

			if (body[plochy[uspplochy[j]].bod2].norz>0)
				i2=body[plochy[uspplochy[j]].bod2].norz*255;
			else
				i2=0;

			if (body[plochy[uspplochy[j]].bod3].norz>0)
				i3=body[plochy[uspplochy[j]].bod3].norz*255;
			else
				i3=0;
					//toto bolo najdenie farieb vo vrcholoch...


			if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;tmp=i1;i1=i2;i2=tmp;}
			if (y2>y3) {tmp=y2;y2=y3;y3=tmp;tmp=x2;x2=x3;x3=tmp;tmp=i2;i2=i3;i3=tmp;}
			if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;tmp=i1;i1=i2;i2=tmp;}
			// toto bolo usporaduvanie podla y suradnice.


			if (abs(y3-y1)>1.2)	{

				// najdenie pomocneho bodu
				xs = (x1*(y2-y3)-x3*(y2-y1))/(y1-y3);
				is = (i1*(y2-y3)-i3*(y2-y1))/(y1-y3);


			   //	if (x2>xstr) {tmp=x2;x2=xstr;xstr=tmp;tmp=istr;istr=i2;i2=tmp;}


				for (yy=y1;yy<=y2;yy++)	{ // horny trojuholnik

					if (abs(y1-y2)<1.2) {
						xl=x1;
						xp=x2;
						il=i1;
						ip=i2;
					}
					else	{
						xl=(x1*(yy-y2)-x2*(yy-y1))/(y1-y2);
						xp=(x1*(yy-y2)-xs*(yy-y1))/(y1-y2);
						il=(i1*(yy-y2)-i2*(yy-y1))/(y1-y2);
						ip=(i1*(yy-y2)-is*(yy-y1))/(y1-y2);
					}

					if (xl>xp) {
						tmp=xl;
						xl=xp;
						xp=tmp;
						tmp=il;il=ip;ip=tmp;
					}


					for (xx=xl;xx<=xp;xx++)	{// podho po riadku !

						if (abs(xl-xp)<2.2)
							in=il;
						else
							in =(il*(xx-xp)-ip*(xx-xl))/(xl-xp);
						if (in>255) in=0;

						putPixel(xx,yy,rgbColor((uchar)(in),0,0));
					}
				}


				for (yy=y3;yy>=y2;yy--)	{ // dolny trojuholnik

					if (abs(y3-y2)<1.2) {
						xl=x2;
						xp=x3;
						il=i2;
						ip=i3;
					}
					else	{
						xl=(x3*(yy-y2)-x2*(yy-y3))/(y3-y2);
						xp=(x3*(yy-y2)-xs*(yy-y3))/(y3-y2);
						il=(i3*(yy-y2)-i2*(yy-y3))/(y3-y2);
						ip=(i3*(yy-y2)-is*(yy-y3))/(y3-y2);
					}

					if (xl>xp) {tmp=xl;xl=xp;xp=tmp;
						tmp=il;il=ip;ip=tmp;}


					for (xx=xl;xx<=xp;xx++)	{// podho po riadku !
						if (abs(xl-xp)<2.2)
							in=il;
						else
							in =(il*(xx-xp)-ip*(xx-xl))/(xl-xp);
						if (in>255) in=0;

						putPixel(xx,yy,rgbColor((uchar)(in),0,0));
					}
				}
			}


			// tu konci Gouraudovanie 1 plochy
	}
	else {// ostra !!!



		float x1,x2,x3,y1,y2,y3;
		float xs;
		float xl,xp;
		int xx,yy,in;
		float tmp;


		x1=100/kamery.zk*(body[plochy[uspplochy[j]].bod1].x+0.25*body[plochy[uspplochy[j]].bod1].z)+xres/2+100/kamery.zk*kamery.xk;
		y1=100/kamery.zk*(body[plochy[uspplochy[j]].bod1].y+0.25*body[plochy[uspplochy[j]].bod1].z)+yres/2+100/kamery.zk*kamery.yk;
		x2=100/kamery.zk*(body[plochy[uspplochy[j]].bod2].x+0.25*body[plochy[uspplochy[j]].bod2].z)+xres/2+100/kamery.zk*kamery.xk;
		y2=100/kamery.zk*(body[plochy[uspplochy[j]].bod2].y+0.25*body[plochy[uspplochy[j]].bod2].z)+yres/2+100/kamery.zk*kamery.yk;
		x3=100/kamery.zk*(body[plochy[uspplochy[j]].bod3].x+0.25*body[plochy[uspplochy[j]].bod3].z)+xres/2+100/kamery.zk*kamery.xk;
		y3=100/kamery.zk*(body[plochy[uspplochy[j]].bod3].y+0.25*body[plochy[uspplochy[j]].bod3].z)+yres/2+100/kamery.zk*kamery.yk;

			// tu zacina vyplnanie 1 plochy
		in=255*plochy[uspplochy[j]].norz;


		if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;}
		if (y2>y3) {tmp=y2;y2=y3;y3=tmp;tmp=x2;x2=x3;x3=tmp;}
		if (y1>y2) {tmp=y1;y1=y2;y2=tmp;tmp=x1;x1=x2;x2=tmp;}
			// toto bolo usporaduvanie podla y suradnice.


		if (abs(y3-y1)>1.0)	{

				// najdenie pomocneho bodu
			xs = (x1*(y2-y3)-x3*(y2-y1))/(y1-y3);

			for (yy=y1;yy<=y2;yy++)	{ // horny trojuholnik

				if (abs(y1-y2)<1.0) {
					xl=x1;
					xp=x2;
				}
				else	{
					xl=(x1*(yy-y2)-x2*(yy-y1))/(y1-y2);
					xp=(x1*(yy-y2)-xs*(yy-y1))/(y1-y2);
				}


				line(xl,yy,xp,yy,rgbColor((uchar)(in),0,0));


			}


			for (yy=y3;yy>=y2;yy--)	{ // dolny trojuholnik

				if (abs(y3-y2)<1.0) {
					xl=x2;
					xp=x3;
				}
				else	{
					xl=(x3*(yy-y2)-x2*(yy-y3))/(y3-y2);
					xp=(x3*(yy-y2)-xs*(yy-y3))/(y3-y2);
				}

				line(xl,yy,xp,yy,rgbColor((uchar)(in),0,0));

			}

		}



		}

		}
	}



klav:

	do
	//a=inportb(0x60);
				{
				asm {				// snimaj kb
					in al,60h
					mov a,al
					in al,61h
					mov ah,al
					or al,80h
					out 61h,al
					xchg ah,al
					out 61h,al
					mov al,20h
					out 20h,al
					mov cx,0x0000    // toto je finta proti
					mov dx,0x041c    // pretekaniu kb. bufra
					mov es,cx
					mov di,dx
					mov ax,es:[di]
					mov dx,0x041a
					mov di,dx
					mov es:[di],ax  // az potadeto
				}

				 }

	while (a>100);

	switch (a)	{

	case 80 : 	for (j=0;j<pocetbodov;j++) {             //rotacia bodov hran okolo x
		z1 = body[j].z*0.995-body[j].y*0.09983;
		y1 = body[j].z*0.09983+body[j].y*0.995;
		body[j].z=z1;
		body[j].y=y1;
	}
		;break;
	case 72 : 	for (j=0;j<pocetbodov;j++) {             //rotacia bodov hran okolo x
		z1 = body[j].z*0.995-body[j].y*-0.09983;
		y1 = body[j].z*-0.09983+body[j].y*0.995;
		body[j].z=z1;
		body[j].y=y1;
	}
		;break;
	case 71 : 	for (j=0;j<pocetbodov;j++) {             //rotacia bodov hran okolo y
		x1 = body[j].x*0.995-body[j].z*0.09983;
		z1 = body[j].x*0.09983+body[j].z*0.995;
		body[j].x=x1;
		body[j].z=z1;
	}
		;break;
	case 73 : 	for (j=0;j<pocetbodov;j++) {             //rotacia bodov hran okolo y
		x1 = body[j].x*0.995-body[j].z*-0.09983;
		z1 = body[j].x*-0.09983+body[j].z*0.995;
		body[j].x=x1;
		body[j].z=z1;
	}
		;break;
	case 77 : 	for (j=0;j<pocetbodov;j++) {             //rotacia bodov hran okolo z
		x1 = body[j].x*0.995-body[j].y*0.09983;
		y1 = body[j].x*0.09983+body[j].y*0.995;

		body[j].x=x1;
		body[j].y=y1;

		}
		;break;
	case 75 : 	for (j=0;j<pocetbodov;j++) {             //rotacia bodov hran okolo z
		x1 = body[j].x*0.995-body[j].y*-0.09983;
		y1 = body[j].x*-0.09983+body[j].y*0.995;

		body[j].x=x1;
		body[j].y=y1;

		}
		;break;
	case 30 : kamery.xk-=0.2;break;
	case 31 : kamery.yk+=0.2;break;
	case 32 : kamery.xk+=0.2;break;
	case 17 : kamery.yk-=0.2;break;
	case 33 : if (kamery.zk>0.3) kamery.zk-=0.2;break;
	case 34 : kamery.zk+=0.2;break;
	case 1  : restoreMode();//PG256_End();
				goto zac;break;
	default : goto klav;break;
	}
	for (j=0;j<pocetploch;j++) {
			normalizuj(j);
			}

tuto:
			vpage = ++vpage % (maxpage+1);
			setVisualPage(vpage);
            apage = ++apage % (maxpage+1);


/*
for (j=0;j<xres/2;j++)
	for (i=0;i<200;i++)
	cleardevice();
	Set256_Pixel(j,i,0);
  */
goto zaciatok;

}

