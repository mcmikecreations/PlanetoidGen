#ifndef LAKE_H
#define LAKE_H

#include "pointset.h"

class Lake
{
public:
	Lake (int id);
	void addPoint (Point& p, int flow);
	bool inLake (Point& p);
	void raiseLake (int newHeight);
	bool lowestShorePoint (Point& p);
	void setOutflowPoint (Point& p)				{ outflowPoint = p; }
	bool getInflowPoint (Point& p);
	bool highestInflowNeighbor(Point& neighbor);
	void displayLake ();

	void Reset_Iterator()					{points.Reset_Iterator();}
	bool Iterate_Next(Point& p)				{return points.Iterate_Next(p); }
	inline int getOutflow()					{return inflow;}
private:
	int id;
	int inflow;
	PointSet points;
	PointSet shore;

	Point inflowPoint;			// of the many possible, this is the point with the largest flow
	Point outflowPoint;

	int maxInflow;

	bool isSurrounded (Point& p);
	bool highestFlowNonLake (Point& p, Point &nonlake);
	bool lowestNonLake (Point& p, Point& low);
};

#endif