#include "WaterNode.h"
#include "params.h"
#include "executive.h"

WaterNode::WaterNode ()
{
	inflow = 1;
	gradient = 0;

	upstream = Point(-1, -1);
	downstream = Point(-1, -1);
	largestFlow = 0;
}

int WaterNode::getHeight()
{
	return Executive::Instance().getHeight(nodePoint);
}

void WaterNode::setPoint (int x, int y)
{
	nodePoint.x = x;
	nodePoint.y = y;
}