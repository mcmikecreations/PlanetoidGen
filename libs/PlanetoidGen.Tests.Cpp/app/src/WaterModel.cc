#include "WaterModel.h"
#include "executive.h"
#include "params.h"
#include "logger.h"
#include "pointset.h"


using namespace std;

std::unique_ptr<WaterModel> WaterModel::_instance;

WaterModel& WaterModel::Instance()
{
	if (_instance.get() == NULL)
	{
		_instance.reset (new WaterModel);
	}

	return *_instance;
}

WaterModel::WaterModel()
{
	Params& params = Params::Instance();

	for (int i = 0; i < params.x_size; i++)
	{
		map.push_back(vector<WaterNode>(params.y_size));
	}

	for (int i = 0; i < params.x_size; i++)
	{
		for (int j = 0; j < params.y_size; j++)
		{
			map[i][j].setPoint (i, j);
		}
	}
}

// lookup a waternode by point
bool WaterModel::lookupNode (Point& p, WaterNode& n)
{
	if (Executive::Instance().onMap(p))
	{
		n = map[p.x][p.y];
		return true;
	}

	return false;
}

// functions which compare WaterNodes.  Used for sorting containers
bool WaterModel::flowCompare (WaterNode& left, WaterNode& right)
{
	if (left.getOutflow() > right.getOutflow())
	{
		return true;
	}
	else
	{
		return false;
	}
}

bool WaterModel::heightCompare (WaterNode& left, WaterNode& right)
{
	if (left.getHeight() > right.getHeight())
	{
		return true;
	}
	else
	{
		return false;
	}
}

void WaterModel::loadVector (std::vector<WaterNode>& dest)
{
	Params& params = Params::Instance();

	for (int i = 0; i < params.x_size; i++)
	{
		dest.insert(dest.end(), map[i].begin(), map[i].end());
	}

	Logger::Instance().Log ("load full vector in WaterModel:  %d points\n", dest.size());
}

void WaterModel::setFlowVectors()
{

	vector<WaterNode> allPoints;
	loadVector(allPoints);

	vector<WaterNode>::iterator iter;

	Logger::Instance().Log ("\n\nSetting flow vectors for %d points\n\n", allPoints.size());

	int sealevelPoints = 0;

	for (iter = allPoints.begin(); iter != allPoints.end(); ++iter)
	{
		Point location;
		WaterNode node = *iter;

		node.getPoint(location);
		int x = location.x;
		int y = location.y;

		if ((location.x == 32) && (location.y == 107))
		{
			Logger::Instance().Log ("breakpoint\n");
		}

		int height = Executive::Instance().getHeight(location);

		// don't need to worry about points at or below sea level
		if (height == 0)
		{
			sealevelPoints++;
			continue;
		}

		int flow = map[x][y].getOutflow();

		visited.insert(location);

		Point next;


		// the lowest point will be "downstream" for this point on the river (and must be lower in elevation than the current point)
		if (! findLowestNotInPath (location, next))
		{
//			Logger::Instance().Log ("start lake at (%d,%d)\n", location.x, location.y);
			int id = startLake(location, next);
			flow = lakes[id].getOutflow();

			if (location == next)
			{
				Logger::Instance().Log ("lake flowing to self\n");
			}

	//		Logger::Instance().Log ("lake %d is fed from (%d,%d)\n", id, next.x, next.y);
	//		lakes[id].displayLake();
	//		printHeightmap ();
			if (! Executive::Instance().onMap(next))
			{
	//			Logger::Instance().Log ("lake flows off of the map\n");
				continue;
			}
		}

		if (! Executive::Instance().onMap(next))
		{
	//		Logger::Instance().Log ("river flows off of the map to (%d,%d)\n", next.x, next.y);
			continue;
		}


		// change the river course to follow the highest flow between nodes
		if (flow >map[next.x][next.y].largestFlow)
		{
			map[next.x][next.y].largestFlow = flow;
			map[next.x][next.y].upstream = location;
			map[next.x][next.y].direction_into = Executive::Instance().directionFrom(location, next);
			map[next.x][next.y].direction_out = map[next.x][next.y].direction_into;
		}

		map[next.x][next.y].addInflow(flow);
		map[x][y].downstream = next;
//		Logger::Instance().Log ("adding %d to flow into (%d,%d)\n", flow, next.x, next.y);
		if (location == next)
		{
			Logger::Instance().Log ("water flowing to self\n");
		}
	}

	Logger::Instance().Log ("%d points at or below sea level\n", sealevelPoints);
}

void WaterModel::printAllFlows ()
{
	int total = 0;
	int ocean = 0;
	Params& params = Params::Instance();

	for (int i = 0; i < params.x_size; i++)
	{
		for (int j = 0; j < params.y_size; j++)
		{
			Point location(i, j);
			if (!Executive::Instance().on_land(location))
			{
				ocean++;
			//	Logger::Instance().Log ("(%d,%d) not on land\n", i, j);
				continue;
			}

			int flow = map[i][j].getOutflow();
	
			Point downstream = map[i][j].downstream;

			int nextFlow = flowAt(downstream);
			Logger::Instance().Log ("(%d,%d) sending %d water to (%d,%d), which has %d\n", i, j, flow, 
				downstream.x, downstream.y, nextFlow);
			total += flow;
		}
	}

	// total can be larger than the number of vertices, since a single unit of water counts in each point downstream
	Logger::Instance().Log ("moved a total of %d water, %d water areas did not contribute \n", total, ocean);

}

// ===================================================================
// A replacement for the Executive's gradient testing
//
// Attempt to find the lowest neighboring point not in the current path.
// The neighboring point should be allowed to be off of the map, in case we are looking on the edge
//
// Returns true on success (modifying "next").
// Returns false on failure, leaving next unmodified.
// ===================================================================

bool WaterModel::findLowestNotInPath (Point& current, Point& next)
{
	Point neighbor;

	int currentHeight = Executive::Instance().getHeight(current);
	int lowestHeight = 100000;
	Point bestNeighbor;

	//Logger::Instance().Log ("searching for next point, starting at (%d,%d)\n", current.x, current.y);

	// check all neighboring points for something lower
	for (int i = 0; i < 8; i++)
	{
		Executive::Instance().StepDir (current, neighbor, i);

		// skip over neighbors which we have visited
		if (visited.in_set(neighbor))
		{
	//		Logger::Instance().Log ("ignoring point (%d,%d) since it has been visited\n", neighbor.x, neighbor.y);
			continue;
		}


		int neighborHeight = Executive::Instance().getHeight(neighbor);
		if (neighborHeight < lowestHeight)
		{
			lowestHeight = neighborHeight;
			bestNeighbor = neighbor;
		}

		//Logger::Instance().Log ("consider (%d,%d) at height %d\n", neighbor.x, neighbor.y, neighborHeight);
	}

	if (lowestHeight < currentHeight)
	{
		next = bestNeighbor;
		return true;
	}

	return false;
}

// lake:
// raise a point to that of the lowest adjacent point
// we want this adjacent point to begin moving water elsewhere

int WaterModel::startLake (Point& p, Point& outflow)
{
	Point lowest;
	Point inflow;
	int height = Executive::Instance().getHeight(p);

	int id = lakes.size();

	Lake newLake(id);
	lakes.push_back(newLake);

	//printHeightmap ();

	Point downstream;

	lowest = p;
	while (1)
	{
		// pass along the flow into p
		Point p;
		int flow;

		// add point "lowest" to the lake, and raise the height of the entire lake

		if (highestInflowNeighbor(id, lowest, p))
		{
			flow = flowAt(p);
			//Logger::Instance().Log ("highest inflow into (%d,%d) is %d through %d,%d\n",
			//	lowest.x, lowest.y, flow, p.x, p.y);
		}
		else
		{
			Logger::Instance().Log ("unable to find the inflow into point (%d,%d)\n", lowest.x, lowest.y);
			flow = 0;
		}

		lakes[id].addPoint(lowest, flow);

	//	lake.inflow += flowAt(lowest);

		// scan the shoreline for outflow
		if (lakes[id].lowestShorePoint(downstream))
		{
			if (! Executive::Instance().onMap(downstream))
			{
				//Logger::Instance().Log ("lake %d empties off the map\n", id);
				inflow = downstream;
				return id;
			}

			//Logger::Instance().Log ("possible overflow point at (%d,%d)\n", downstream.x, downstream.y);
			// by definition the point "downstream" is not currently in the lake.  However we need to
			// check if it is currently flowing into a lake point (and therefore needs to be added)

			Point nextPoint;		// where the downstream point is flowing
			if (downstreamPoint(downstream, nextPoint))
			{
				//Logger::Instance().Log ("lake %d considers overflowing towards(%d,%d)\n", id, nextPoint.x,
				//	nextPoint.y);
				int height = Executive::Instance().getHeight(downstream);

				// a point was found
				if (! lakes[id].inLake(nextPoint))
				{
					//Logger::Instance().Log ("lake %d empties out of %d,%d to %d,%d\n",
					//	id, downstream.x, downstream.y, nextPoint.x, nextPoint.y);
					lakes[id].setOutflowPoint(nextPoint);
					if (! lakes[id].getInflowPoint(inflow))
					{
						Logger::Instance().Log ("unknown inflow point for this lake\n");
					}
					
					lakes[id].raiseLake(height);

					if (lakes[id].highestInflowNeighbor(inflow))
					{
						setLakeDownstream (id, downstream);
						setLakeUpstream (id, inflow);
					}
					else
					{
						Logger::Instance().Log ("unable to find inflow/upstream point for lake\n");
					}

					outflow = downstream;
					return id;
				}
				else
				{
					// downstream is *not* in the lake, but flows back in
					Logger::Instance().Log ("lake point %d,%d flows downstream to (%d,%d) which is inside the lake\n",
						downstream.x, downstream.y, nextPoint.x, nextPoint.y);

					int height = Executive::Instance().getHeight(downstream);
					int height2 = Executive::Instance().getHeight(nextPoint);
					Logger::Instance().Log ("h1 = %d, h2 = %d\n", height, height2);
					checkLakesForPoint (nextPoint);
					lakes[id].displayLake();

					// add the point to the lake, and recalculate the overflow point
					lowest = downstream;
				}

				// the downstream point also empties into the lake
				lowest = downstream;
				//Logger::Instance().Log ("point %d,%d empties into the lake\n", lowest.x, lowest.y);
			//	printHeightmap ();
			}
			else
			{
				int height = Executive::Instance().getHeight(downstream);
				lakes[id].raiseLake(height);
				if (! lakes[id].getInflowPoint(inflow))
				{
					Logger::Instance().Log ("unknown inflow point for this lake\n");
				}

				if (lakes[id].highestInflowNeighbor(inflow))
				{
					setLakeDownstream (id, downstream);
					setLakeUpstream (id, inflow);
				}
				else
				{
					Logger::Instance().Log ("unable to find inflow/upstream point for lake\n");
				}

				outflow = downstream;
				//Logger::Instance().Log ("(%d,%d) flows off the map\n", downstream.x, downstream.y);
				return id;
			}
		}
		else
		{
			Logger::Instance().Log ("no lowestShorePoint found\n");
			return id;
		}
	}

	Logger::Instance().Log ("lake %d may be growing without bound\n", id);
	return id;
}

bool WaterModel::highestInflowNeighbor(int lake_id, Point& src, Point& neighbor)
{
	int maxflow = 0;
	Point bestPoint;

	// find the highest inflow from a neighboring non-lake point
	for (int dir = 0; dir < 8; dir++)
	{
		Point p;
		Executive::Instance().StepDir(src, p, dir);

		if (! lakes[lake_id].inLake(p))
		{
			int flow = flowAt(p);
			if (flow > maxflow)
			{
				maxflow = flow;
				bestPoint = p;
			}
		}
	}

	if (maxflow > 0)
	{
		neighbor = bestPoint;
		return true;
	}

	return false;
}

// find the downstream point of "src"
// returns true if a point was found (and downstream is updated)
// otherwise returns false if "src" flows into the ocean or off the map

bool WaterModel::downstreamPoint (Point& src, Point& downstream)
{
	if (Executive::Instance().onMap(src))
	{
		downstream = map[src.x][src.y].downstream;
		return true;
	}

	return false;
}

int WaterModel::flowAt (Point& p)
{
	if (! Executive::Instance().onMap(p))
	{
		// Logger::Instance().Log ("flow check for point %d,%d which is off of the map\n", p.x, p.y);
		return 0;
	} 

	return map[p.x][p.y].getOutflow();
}

void WaterModel::setLakeDownstream(int lake_id, Point& down)
{
	Point p;

	lakes[lake_id].Reset_Iterator();
	while (lakes[lake_id].Iterate_Next(p))
	{
		map[p.x][p.y].downstream = down;
	}
}

void WaterModel::setLakeUpstream(int lake_id, Point& up)
{
	Point p;

	lakes[lake_id].Reset_Iterator();
	while (lakes[lake_id].Iterate_Next(p))
	{
		map[p.x][p.y].upstream = up;
	}
}

void WaterModel::propagateDownStream (Point& p, int flow)
{
	Point downstream = map[p.x][p.y].downstream;
	Point end(-1, -1);

	while (downstream != end)
	{
		Logger::Instance().Log ("propagate visits (%d,%d)\n", downstream.x, downstream.y);

		downstream = map[downstream.x][downstream.y].downstream;
	}
}

void WaterModel::checkLakesForPoint (Point& p)
{
	Logger::Instance().Log ("point (%d,%d) is in the following lakes: ", p.x, p.y);

	int size = (int) lakes.size();

	for (int i = 0; i < size; i++)
	{
		if (lakes[i].inLake(p))
		{
			Logger::Instance().Log (" %d ", i);
		}
	}
	Logger::Instance().Log ("\n");
}