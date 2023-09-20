#ifndef ACTION_H
#define ACTION_H

#include "map.h"
#include "point.h"
#include "pointset.h"
#include "heightmap.h"

#if 0
// directional constants

const int DIR_UP = 0;
const int DIR_DOWN = 1;
const int DIR_LEFT = 2;
const int DIR_RIGHT = 3;
const int DIR_UR = 4;
const int DIR_UL = 5;
const int DIR_LL = 6;
const int DIR_LR = 7;
#endif

#define HISTORY_SIZE 3

class Action
{
private:
	Point attractor;
	Point repulsor;
	Point seed_pt;
	Point history[HISTORY_SIZE];
	PointSet bset;
	unsigned int history_size;
	unsigned char direction;
	char buffer[100];
	unsigned long action_size;

	Map *mask;

	long value;

	static int count;
	int id;
	int parent;

	// find an active point, starting search at src
	bool find_active_on_ray (Point &src, Point& result);
	bool random_active (Point& p);

	void Remove (Point& p);
	int towardsCenter(Point& p);

	void Remove_Invalid_Neighbors (Point& p);
	void PlotPixels ();
	void expand_pt (Point& p);
	long Score (Point& p);
	long DistSqr (Point& a, Point& b);
	bool Hist_Point (Point& p);

	bool is_active (Point& p, bool debug=false);
	bool OnMap (Point& p);

	bool on_boundary (int x, int y);

	void dump_active ();

	long Dist_to_Edge (Point& p);
	bool in_range (Point& p);
	void Display_Region (Point& p, int range = 3);
public:
	Action (Map *m, int dir, Point& seed, int size, int _parent = 0);
	~Action ();

	void Add (Point &p);


	bool StepDir (Point& src, Point& dst, int dir, int delta = 1);
	void generate ();

	Point *Get_Boundary (unsigned int pos);
};


#endif
