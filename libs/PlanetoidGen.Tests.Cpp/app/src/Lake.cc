#include "Lake.h"
#include "logger.h"
#include "executive.h"
#include <set>
#include <limits>
#include "WaterModel.h"

using namespace std;

Lake::Lake (int lake_id)
{
	inflow = 0;
	maxInflow = 0;

	id = lake_id;
}

void Lake::addPoint(Point& p, int flow)
{ 
	if (points.in_set(p))
	{
		Logger::Instance().Log ("Attempt to add a point (%d,%d) to lake %d but is already in the lake\n",
			p.x, p.y, id);
		Logger::Instance().Log ("%d points in this lake\n", points.size());
		return;
	}

	points.insert(p);

	// new points which have non-lake neighbors are on the shore
	if (! isSurrounded(p))
	{
		//Logger::Instance().Log ("adding (%d,%d) to lake %d's shoreline\n", p.x, p.y, id);
		shore.insert(p);
		inflow += flow;

		if (flow > maxInflow)
		{
			WaterNode node;
			WaterModel::Instance().lookupNode(p, node);

			//Logger::Instance().Log ("this is the max inflow point\n");
			maxInflow = flow;
			inflowPoint = p;
		}
	}
	else
	{
		Logger::Instance().Log ("Lake::addPoint -- point (%d,%d) appears surrounded by other lake points\n",
			p.x, p.y);
	}
}

bool Lake::highestInflowNeighbor(Point& neighbor)
{
	int highFlow = 0;
	Point bestPoint;
	Point p;

	shore.Reset_Iterator();

	// for each point "p" on the lakeshore

	while (shore.Iterate_Next(p))
	{
		Point nonLake;

		if (highestFlowNonLake (p, nonLake))
		{
			int flow = WaterModel::Instance().flowAt(nonLake);

			if (flow > highFlow)
			{
				highFlow = flow;
				bestPoint = nonLake;
			}
		}
	}

	// if we found one neighboring point with a positive flow
	if (highFlow > 0)
	{
		neighbor = bestPoint;
		return true;
	}

	return false;
}

bool Lake::getInflowPoint(Point& p)
{
	if (maxInflow > 0)
	{
		p = inflowPoint;
		return true;
	}

	return false;
}

// check all points on the shore for non-lake neighbors, and return the height of the lowest point
// this will either be an outflow (currently flowing away from the lake), or will need to be added to the lake
// When raising the height of a lake, it will always be to the lowestShoreHeight

bool Lake::lowestShorePoint(Point& lowPointAt)
{
	Point p;
	std::set<Point> remove;		// points scheduled to be removed from the shoreline
	int lowHeight = numeric_limits<int>::max();
	Point lowPoint;

	//Logger::Instance().Log ("attempt to find lowestShorePoint for lake %d, %d points in shore\n",
	//	id, shore.size());

	shore.Reset_Iterator();

	// for each point "p" on the lakeshore

	while (shore.Iterate_Next(p))
	{
		if (isSurrounded(p))
		{
			remove.insert(p);
			continue;
		}

		Point nonLake;

		if (lowestNonLake (p, nonLake))
		{
			int height = Executive::Instance().getHeight(nonLake);

			if (height < lowHeight)
			{
				lowHeight = height;
				lowPoint = nonLake;
			}
		}
	}

	std::set<Point>::iterator iter;
	for (iter = remove.begin(); iter != remove.end(); ++iter)
	{
		Point p = *iter;
		shore.remove(p.x, p.y);

		Logger::Instance().Log ("removing point (%d,%d) from shoreline set\n", p.x, p.y);
	}


	if (lowHeight < numeric_limits<int>::max())
	{
		//Logger::Instance().Log ("lowest shoreline height is %d at %d,%d\n", lowHeight, lowPoint.x, lowPoint.y);

		lowPointAt = lowPoint;
		return true;
	}

	return false;
}

bool Lake::lowestNonLake(Point &src, Point& lowPointAt)
{
	Point neighbor;
	int lowHeight = numeric_limits<int>::max();
	Point lowPoint;

	for (int dir = 0; dir < 8; dir++)
	{
		Executive::Instance().StepDir(src, neighbor, dir);

		if (! inLake(neighbor))
		{
			int height = Executive::Instance().getHeight(neighbor);
			if (height < lowHeight)
			{
				lowHeight = height;
				lowPoint = neighbor;
			}
		}
	}

	if (lowHeight < numeric_limits<int>::max())
	{
		//Logger::Instance().Log ("lowestNonLake: %d,%d at height %d\n", lowPoint.x, lowPoint.y, lowHeight);

		lowPointAt = lowPoint;
		return true;
	}

	return false;
}

bool Lake::highestFlowNonLake(Point &src, Point& highFlowAt)
{
	Point neighbor;
	int highFlow = 0;
	Point bestPoint;

	for (int dir = 0; dir < 8; dir++)
	{
		Executive::Instance().StepDir(src, neighbor, dir);

		if (! inLake(neighbor))
		{
			int flow = WaterModel::Instance().flowAt(neighbor);

			if (flow > highFlow)
			{
				highFlow = flow;
				bestPoint = neighbor;
			}
		}
	}

	if (highFlow > 0)
	{
		highFlowAt = bestPoint;
		return true;
	}

	return false;
}

bool Lake::inLake (Point& p)
{
	return points.in_set(p);
}

// is "p" an interior point in the lake (ie surrounded by other lake points)?
// shoreline points are the only ones which have inflow/outflow.  They are the only points we need
// to check when raising the lake height.

bool Lake::isSurrounded(Point& p)
{
	Point neighbor;

	for (int dir = 0; dir < 8; dir++)
	{
		Executive::Instance().StepDir(p, neighbor, dir);
		if (! inLake(neighbor))
		{
			return false;
		}
	}

	return true;
}

void Lake::raiseLake (int newHeight)
{
	points.Reset_Iterator ();

	Point p;

	Logger::Instance().Log ("lake %d: raising %d points to %d\n", id, points.size(), newHeight);

	while (points.Iterate_Next(p))
	{
		Executive::Instance().setHeight (p, newHeight);
	}
}

void Lake::displayLake ()
{
	Logger::Instance().Log ("Lake %d:  (%d points)", id, points.size());

	points.Reset_Iterator();
	Point p;

	while (points.Iterate_Next(p))
	{
		Logger::Instance().Log (" (%d,%d) ", p.x, p.y);
	}

	Logger::Instance().Log ("\n");
}