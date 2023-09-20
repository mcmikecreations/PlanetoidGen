#ifndef WATERNODE_H
#define WATERNODE_H

#include "point.h"

class WaterNode
{
public:
	WaterNode ();

	inline void setGradient(int grad)				{ gradient = grad; }
	inline int getGradient ()						{ return gradient; }
	inline void addInflow (int flow)				{ inflow += flow; }
	inline int getOutflow ()						{ return inflow; }
	void setPoint (int x, int y);
	inline void getPoint (Point& p)					{ p = nodePoint; }
	int getHeight();
	Point upstream;
	Point downstream;
	int direction_into;								// direction of flow into this node
	int direction_out;
	int largestFlow;								// largest single flow into this node (determines direction)

private:
	Point nodePoint;

	int gradient;
	int inflow;
};
#endif