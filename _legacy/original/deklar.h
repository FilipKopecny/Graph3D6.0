#ifndef DEKLAR_H
#define DEKLAR_H

#include<math.h>
#include<dos.h>
#include<conio.h>
#include<string.h>
#include<ctype.h>
#include<stdlib.h>
#include<stdio.h>


typedef struct {
	double x,y,z,norx,nory,norz;
	} bod; // sem by sa este zislo dat smerniky na plochy, ale uz mam
			// spravenu tu pomalu proceduru(voidfciu), takze...

typedef struct {
	double x,y,z,dx,dy,dz,xuhol,yuhol,zuhol;
	} kvader;

typedef struct {
	double x,y,z,xuhol,yuhol,zuhol;
	bod b[5][5];
	} meshbs;

typedef struct {
	double x,y,z,xuhol,yuhol,zuhol;
	bod b[7][7];
	} meshbe;

typedef struct {
	float x,y,z,rin,rout,xuhol,yuhol,zuhol;
	int xjemnost,yjemnost;
	} toroid;

typedef struct {
	double xk,yk,zk,uhol;
	} kamera;

typedef struct {
	int bod1, bod2;
	} hrana;

typedef struct {
	int bod1, bod2, bod3;
	double norx,nory,norz;
	int zaobli;
	} plocha;


float Vynasobzbe(float c1u,float c2u,float c3u,float c4u,meshbe *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy);
float Vynasobybe(float c1u,float c2u,float c3u,float c4u,meshbe *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy);
float Vynasobxbe(float c1u,float c2u,float c3u,float c4u,meshbe *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy);
float Vynasobzbs(float c1u,float c2u,float c3u,float c4u,meshbs *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy);
float Vynasobybs(float c1u,float c2u,float c3u,float c4u,meshbs *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy);
float Vynasobxbs(float c1u,float c2u,float c3u,float c4u,meshbs *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy);

void normalizuj (int p);
void FillTorus(float rc, int numc, float rt, int numt);
void Bspline(int i);
void Bezier(int i);
void Usporiadaj (void);
int PocetUspPrivrPloch;



bod far *body;
hrana far *hrany;
plocha far *plochy;

int uspplochy[1300];


kvader kvadre[5];
toroid toroidy[5];
meshbs meshebs[5];
meshbe meshebe[5];
kamera kamery;

int pocetbodov=0;
int pocethran=0;
int pocetploch=0;
int pb=0;
int ph=0;
int pp=0;
int maxpage,yres,xres;
int apage,vpage;

void normalizuj(int p)
{
double v1[3];
double v2[3];

double min;

		v1[0]=body[plochy[p].bod2].x-body[plochy[p].bod1].x;
		v1[1]=body[plochy[p].bod2].y-body[plochy[p].bod1].y;
		v1[2]=body[plochy[p].bod2].z-body[plochy[p].bod1].z;

		v2[0]=body[plochy[p].bod3].x-body[plochy[p].bod1].x;
		v2[1]=body[plochy[p].bod3].y-body[plochy[p].bod1].y;
		v2[2]=body[plochy[p].bod3].z-body[plochy[p].bod1].z;

		plochy[p].norx = v1[1]*v2[2]-v2[1]*v1[2];		// normala
		plochy[p].nory = v1[2]*v2[0]-v2[2]*v1[0];
		plochy[p].norz = v1[0]*v2[1]-v2[0]*v1[1];

		min=sqrt(plochy[p].norx*plochy[p].norx + plochy[p].nory*plochy[p].nory + plochy[p].norz*plochy[p].norz);

		if (min!=0){
			plochy[p].norx /= min;
			plochy[p].nory /= min;
			plochy[p].norz /= min;
		}
}


float Vynasobzbs (float c1u,float c2u,float c3u,float c4u,meshbs *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy)
{
float temp[4],tmp;

temp[0]=c1u*meshe->b[xxx][yyy].z+c2u*meshe->b[xxx][yyy+1].z+c3u*meshe->b[xxx][yyy+2].z+c4u*meshe->b[xxx][yyy+3].z;
temp[1]=c1u*meshe->b[xxx+1][yyy].z+c2u*meshe->b[xxx+1][yyy+1].z+c3u*meshe->b[xxx+1][yyy+2].z+c4u*meshe->b[xxx+1][yyy+3].z;
temp[2]=c1u*meshe->b[xxx+2][yyy].z+c2u*meshe->b[xxx+2][yyy+1].z+c3u*meshe->b[xxx+2][yyy+2].z+c4u*meshe->b[xxx+2][yyy+3].z;
temp[3]=c1u*meshe->b[xxx+3][yyy].z+c2u*meshe->b[xxx+3][yyy+1].z+c3u*meshe->b[xxx+3][yyy+2].z+c4u*meshe->b[xxx+3][yyy+3].z;

tmp = (c1v*temp[0]+c2v*temp[1]+c3v*temp[2]+c4v*temp[3]);

return tmp;

}


float Vynasobybs(float c1u,float c2u,float c3u,float c4u,meshbs *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy)
{
float temp[4],tmp;

temp[0]=c1u*meshe->b[xxx][yyy].y+c2u*meshe->b[xxx][yyy+1].y+c3u*meshe->b[xxx][yyy+2].y+c4u*meshe->b[xxx][yyy+3].y;
temp[1]=c1u*meshe->b[xxx+1][yyy].y+c2u*meshe->b[xxx+1][yyy+1].y+c3u*meshe->b[xxx+1][yyy+2].y+c4u*meshe->b[xxx+1][yyy+3].y;
temp[2]=c1u*meshe->b[xxx+2][yyy].y+c2u*meshe->b[xxx+2][yyy+1].y+c3u*meshe->b[xxx+2][yyy+2].y+c4u*meshe->b[xxx+2][yyy+3].y;
temp[3]=c1u*meshe->b[xxx+3][yyy].y+c2u*meshe->b[xxx+3][yyy+1].y+c3u*meshe->b[xxx+3][yyy+2].y+c4u*meshe->b[xxx+3][yyy+3].y;

tmp = (c1v*temp[0]+c2v*temp[1]+c3v*temp[2]+c4v*temp[3]);

return tmp;

}

float Vynasobxbs(float c1u,float c2u,float c3u,float c4u,meshbs *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy)
{
float temp[4],tmp;

temp[0]=c1u*meshe->b[xxx][yyy].x+c2u*meshe->b[xxx][yyy+1].x+c3u*meshe->b[xxx][yyy+2].x+c4u*meshe->b[xxx][yyy+3].x;
temp[1]=c1u*meshe->b[xxx+1][yyy].x+c2u*meshe->b[xxx+1][yyy+1].x+c3u*meshe->b[xxx+1][yyy+2].x+c4u*meshe->b[xxx+1][yyy+3].x;
temp[2]=c1u*meshe->b[xxx+2][yyy].x+c2u*meshe->b[xxx+2][yyy+1].x+c3u*meshe->b[xxx+2][yyy+2].x+c4u*meshe->b[xxx+2][yyy+3].x;
temp[3]=c1u*meshe->b[xxx+3][yyy].x+c2u*meshe->b[xxx+3][yyy+1].x+c3u*meshe->b[xxx+3][yyy+2].x+c4u*meshe->b[xxx+3][yyy+3].x;

tmp = (c1v*temp[0]+c2v*temp[1]+c3v*temp[2]+c4v*temp[3]);

return tmp;

}



float Vynasobzbe (float c1u,float c2u,float c3u,float c4u,meshbe *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy)
{
float temp[4],tmp;

temp[0]=c1u*meshe->b[xxx][yyy].z+c2u*meshe->b[xxx][yyy+1].z+c3u*meshe->b[xxx][yyy+2].z+c4u*meshe->b[xxx][yyy+3].z;
temp[1]=c1u*meshe->b[xxx+1][yyy].z+c2u*meshe->b[xxx+1][yyy+1].z+c3u*meshe->b[xxx+1][yyy+2].z+c4u*meshe->b[xxx+1][yyy+3].z;
temp[2]=c1u*meshe->b[xxx+2][yyy].z+c2u*meshe->b[xxx+2][yyy+1].z+c3u*meshe->b[xxx+2][yyy+2].z+c4u*meshe->b[xxx+2][yyy+3].z;
temp[3]=c1u*meshe->b[xxx+3][yyy].z+c2u*meshe->b[xxx+3][yyy+1].z+c3u*meshe->b[xxx+3][yyy+2].z+c4u*meshe->b[xxx+3][yyy+3].z;

tmp = (c1v*temp[0]+c2v*temp[1]+c3v*temp[2]+c4v*temp[3]);

return tmp;

}


float Vynasobybe(float c1u,float c2u,float c3u,float c4u,meshbe *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy)
{
float temp[4],tmp;

temp[0]=c1u*meshe->b[xxx][yyy].y+c2u*meshe->b[xxx][yyy+1].y+c3u*meshe->b[xxx][yyy+2].y+c4u*meshe->b[xxx][yyy+3].y;
temp[1]=c1u*meshe->b[xxx+1][yyy].y+c2u*meshe->b[xxx+1][yyy+1].y+c3u*meshe->b[xxx+1][yyy+2].y+c4u*meshe->b[xxx+1][yyy+3].y;
temp[2]=c1u*meshe->b[xxx+2][yyy].y+c2u*meshe->b[xxx+2][yyy+1].y+c3u*meshe->b[xxx+2][yyy+2].y+c4u*meshe->b[xxx+2][yyy+3].y;
temp[3]=c1u*meshe->b[xxx+3][yyy].y+c2u*meshe->b[xxx+3][yyy+1].y+c3u*meshe->b[xxx+3][yyy+2].y+c4u*meshe->b[xxx+3][yyy+3].y;

tmp = (c1v*temp[0]+c2v*temp[1]+c3v*temp[2]+c4v*temp[3]);

return tmp;

}

float Vynasobxbe(float c1u,float c2u,float c3u,float c4u,meshbe *meshe,float c1v, float c2v,float c3v,float c4v,int xxx,int yyy)
{
float temp[4],tmp;

temp[0]=c1u*meshe->b[xxx][yyy].x+c2u*meshe->b[xxx][yyy+1].x+c3u*meshe->b[xxx][yyy+2].x+c4u*meshe->b[xxx][yyy+3].x;
temp[1]=c1u*meshe->b[xxx+1][yyy].x+c2u*meshe->b[xxx+1][yyy+1].x+c3u*meshe->b[xxx+1][yyy+2].x+c4u*meshe->b[xxx+1][yyy+3].x;
temp[2]=c1u*meshe->b[xxx+2][yyy].x+c2u*meshe->b[xxx+2][yyy+1].x+c3u*meshe->b[xxx+2][yyy+2].x+c4u*meshe->b[xxx+2][yyy+3].x;
temp[3]=c1u*meshe->b[xxx+3][yyy].x+c2u*meshe->b[xxx+3][yyy+1].x+c3u*meshe->b[xxx+3][yyy+2].x+c4u*meshe->b[xxx+3][yyy+3].x;

tmp = (c1v*temp[0]+c2v*temp[1]+c3v*temp[2]+c4v*temp[3]);

return tmp;

}


void FillTorus(float rc, int numc, float rt, int numt)
{
	int i, j, k;
	double s, t;
	double x, y, z;
	double pi, twopi;

	pi = 3.14159265358979323846;
	twopi = 2 * pi;

	for (i = 0; i < numc; i++) {


		for (j = 1; j <= numt; j++) {
			for (k = 1; k >= 0; k--) {
				s = (i + k) % numc + 0.5;
				t = j % numt;


			}
				body[pocetbodov].x = (rt + rc * cos(s*twopi/numc)) * cos(t*twopi/numt);
				body[pocetbodov].y = (rt + rc * cos(s*twopi/numc)) * sin(t*twopi/numt);
				body[pocetbodov++].z = rc * sin(s*twopi/numc);

		}
	}
}



void Bezier(int i)
{

float c1u,c2u,c3u,c4u,c1v,c2v,c3v,c4v,u,v;
int xxx,yyy;

for (xxx=0;xxx<=4;xxx+=3)
for (yyy=0;yyy<=4;yyy+=3)
{
for (u=0;u<=1;u+=0.2)	{

	if(yyy==3 && u==0) u=0.2;

	c1u=(1-u)*(1-u)*(1-u);
	c2u=3*u*(1-u)*(1-u);
	c3u=3*u*u*(1-u);
	c4u=u*u*u;

	for (v=0;v<=1;v+=0.2)	{

	if(xxx==3 && v==0) v=0.2;

	c1v=(1-v)*(1-v)*(1-v);
	c2v=3*v*(1-v)*(1-v);
	c3v=3*v*v*(1-v);
	c4v=v*v*v;

		body[pocetbodov].x=Vynasobxbe(c1u,c2u,c3u,c4u,&meshebe[i],c1v,c2v,c3v,c4v,xxx,yyy);
		body[pocetbodov].y=Vynasobybe(c1u,c2u,c3u,c4u,&meshebe[i],c1v,c2v,c3v,c4v,xxx,yyy);
		body[pocetbodov++].z=Vynasobzbe(c1u,c2u,c3u,c4u,&meshebe[i],c1v,c2v,c3v,c4v,xxx,yyy);

		}
	}
}
}



void Bspline(int i)
{

float c1u,c2u,c3u,c4u,c1v,c2v,c3v,c4v,u,v;
int xxx,yyy;

for (xxx=0;xxx<=1;xxx++)
for (yyy=0;yyy<=1;yyy++)
{
	for (u=0;u<=1;u+=0.2)	{

		if(yyy==1 && u==0) u=0.2;

		c1u=(1-u)*(1-u)*(1-u);
		c2u=3*u*u*u-6*u*u+4;
		c3u=-3*u*u*u+3*u*u+3*u+1;
		c4u=u*u*u;

		for (v=0;v<=1;v+=0.2)	{

			if(xxx==1 && v==0) v=0.2;

			c1v=(1-v)*(1-v)*(1-v);
			c2v=3*v*v*v-6*v*v+4;
			c3v=-3*v*v*v+3*v*v+3*v+1;
			c4v=v*v*v;

			body[pocetbodov].x =Vynasobxbs(c1u,c2u,c3u,c4u,&meshebs[i],c1v,c2v,c3v,c4v,xxx,yyy)/36;
			body[pocetbodov].y =Vynasobybs(c1u,c2u,c3u,c4u,&meshebs[i],c1v,c2v,c3v,c4v,xxx,yyy)/36;
			body[pocetbodov++].z =Vynasobzbs(c1u,c2u,c3u,c4u,&meshebs[i],c1v,c2v,c3v,c4v,xxx,yyy)/36;
		}
	}
}
}


float avz (int p)
{
	float temp;
	temp = (body[plochy[p].bod1].z + body[plochy[p].bod2].z + body[plochy[p].bod3].z)/3;
	return temp;
}


void Usporiadaj (void)
{
int p,i,j;
int pridal=0;

for (p=0;p<pocetploch;p++)
	uspplochy[p]=-1;
PocetUspPrivrPloch=0;

for (p=0;p<pocetploch;p++)	{	// chod cez vsetky plochy

	if (plochy[p].norz>0)	{	// vyber len privratene

		for (i=0;i<PocetUspPrivrPloch;i++) {

			if (avz(p) < avz(uspplochy[i])) {
	  // ak zsuradnica plochy < zsuradnica plochy v poli usporiadanych ploch
				for (j=PocetUspPrivrPloch;j>=i;j--) //urob miesto
					uspplochy[j+1]=uspplochy[j];
				// a suchni ju tam !
				uspplochy[i]=p; pridal = 1;
				break; // vylez z cyklu
			}
		}

		if (i==PocetUspPrivrPloch)  {
			uspplochy[i]=p;pridal=1;
		}

		if (pridal)	{
			PocetUspPrivrPloch++;
			pridal=0;
		}
	}
}
}

void dajnormalybodom()	// toto je straaasne pomale... Nuz co !
{
int kolkympatri,i,j; float norz,norx,nory;
float pod;

for (i=0;i<pocetbodov;i++)	{
	norz=0.0;norx=0.0;nory=0.0;kolkympatri=0;
	for (j=0;j<pocetploch;j++)	{
		if (i==plochy[j].bod1 || i==plochy[j].bod2 || i==plochy[j].bod3)	{
			norz+=plochy[j].norz;
			nory+=plochy[j].nory;
			norx+=plochy[j].norx;
			kolkympatri++;
		}
	}
	if (kolkympatri) {
		pod=sqrt(norx*norx + nory*nory + norz*norz);
		if (pod!=0)	{
			body[i].norz=norz/pod;
			body[i].nory=nory/pod;
			body[i].norx=norx/pod;
		}
	}
}
}




int testDirectColor(long colors)
{
	int     i,choice,maxmenu,numbits;
	int     bitsperpixel,memmodel;
	long    pagesize;
	int     menu[10];

		if (colors == 0x7FFFL)
			numbits = 15;
		else if (colors == 0xFFFFL)
			numbits = 16;
		else numbits = 24;

		maxmenu = 0;

		for (i = 0; modeList[i] != -1; i++) {
			/* Filter out the appropriate video modes */

			if (!getSuperVGAModeInfo(modeList[i],&xres,&yres,&bytesperline,
					&bitsperpixel,&memmodel,&maxpage,&pagesize))
				continue;
			if (bitsperpixel == numbits || (numbits == 24 && bitsperpixel == 32)) {
				printf("    [%d] - %d x %d %ld color (%d page)\n",maxmenu,
					xres,yres,colors+1,maxpage+1);
				menu[maxmenu++] = modeList[i];
				}
			}
		choice = getch();
		choice -= '0';
		if (0 <= choice && choice < maxmenu) {
			if (!setSuperVGAMode(menu[choice])) {
				printf("\n");
				printf("ERROR: Video mode did not set correctly!\n\n");
				printf("\nPress any key to continue...\n");
				getch();
				return 0;
				}
			else {
			getSuperVGAModeInfo(menu[choice],&xres,&yres,&bytesperline,
					&bitsperpixel,&memmodel,&maxpage,&pagesize);
			return 1;

				}
			}
return 0;
}




#endif
