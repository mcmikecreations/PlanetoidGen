#include "ErosionAgent.h"
#include "params.h"
#include "logger.h"
#include "executive.h"
#include <algorithm>
#include "Widener.h"
#include <map>
#include "Lake.h"

const int minimum_flow = 10;

using namespace std;
bool flowCompare (TreeNode& left, TreeNode& right);

typedef std::map<int,int> LakeMap;			// map point id to lake index
LakeMap lakeOf;
std::vector<Lake> lakes;

ErosionAgent::ErosionAgent ()
{
	Params& params = Params::Instance();
	int size = params.x_size * params.y_size;

	forest.reserve (size);
	type = EROSION_AGENT;

	int index = 0;

	for (int j = 0; j < params.y_size; j++)
	{
		for (int i = 0; i < params.x_size; i++)
		{
			TreeNode node;

			int id = indexOf(i, j);
			node.setID(id);

			forest.push_back(node);
		}
	}

	current_x = 0;
	current_y = 0;
	buildingForests = true;
	flowPosition = -1;
	runnable = false;
}

void ErosionAgent::addNode(Point &p, TreeNode &node)
{
	int index = indexOf(p);

	Logger::Instance().Log ("point %d,%d has index %d\n", p.x, p.y, index);
}

int ErosionAgent::indexOf(int x, int y)
{
	Params& params = Params::Instance();
	int index = params.x_size * y + x;
	return index;
}

// ===================================================================
// convert an id into a Point
//
// An "id" is an index into a TreeNode vector, and is turned into a
// map coordinate.
// ===================================================================

void ErosionAgent::pointOf(int id, Point& p)
{
	Params& params = Params::Instance();

	p.x = id % params.x_size;
	p.y = id / params.x_size;
}

// ===================================================================
// Perform one time unit of work
//
// The ErosionAgent will start by building a spanning forest
// ===================================================================

bool ErosionAgent::Execute()
{
	Logger::Instance().Log ("starting spanning forests at %s\n", Executive::Instance().currentTime().c_str());
	buildSpanningForests ();
	Logger::Instance().Log ("starting flowing water at %s\n", Executive::Instance().currentTime().c_str());

	flowWater ();
	Logger::Instance().Log ("ending at %s\n", Executive::Instance().currentTime().c_str());

	return false;
#if 0


	Params& params = Params::Instance();
	int currentIndex = indexOf (current_x, current_y);
	int lastIndex = indexOf (params.x_size-1, params.y_size-1);

	// end of the map has been reached, stop the agent
	if ((buildingForests) && (currentIndex > lastIndex))
	{
		buildingForests = false;
		sortedForest = forest;
		std::sort (sortedForest.begin(), sortedForest.end());
		forest_iterator = sortedForest.begin();
		return true;;
	}
	else if (! buildingForests)
	{
		if (flowPosition == -1)
		{
			// last spanning tree has been walked
			if (forest_iterator == sortedForest.end())
			{
				performTextureWalk ();
				return false;
			}

			flowPosition = forest_iterator->getID();

			Point current;
			pointOf(flowPosition, current);
			Logger::Instance().Log ("\n\n");
			Logger::Instance().Log ("water flow starting at %d (%d,%d)\n", flowPosition, current.x, current.y);


			currentRoot = flowPosition;						// identify the spanning tree by its root
			currentPath.clear();

			++forest_iterator;
		}

		flowPosition = waterFlow (flowPosition);
	}
	else
	{
		buildSpanningForests ();
	}

	return true;
#endif
}

// Which paths lead to the ocean?

void ErosionAgent::buildSpanningForests()
{
	Params& params = Params::Instance();
	PointSet visited;

	int flow;
	sortedForest = forest;
	std::sort (sortedForest.begin(), sortedForest.end());
	std::vector<TreeNode>::iterator iter;
	
	Logger::Instance().Log ("===========================================================\n");

	for (iter = sortedForest.begin(); iter != sortedForest.end(); ++iter)
	{
		TreeNode node = *iter;
		int index = node.getID();

		Point location;
		pointOf(index, location);
		int height = Executive::Instance().getHeight(location);

		// don't need to worry about points at or below sea level
		if (height == 0)
			break;

		flow = forest[index].getOutflow();
	//	Logger::Instance().Log ("finding gradient from (%d,%d) altitude %d, flow in %d\n", location.x, location.y, height, flow);

		visited.insert(location.x, location.y);
		Point next;
		if (! findLowestNotInPath (location, next))
		{
			Logger::Instance().Log ("start lake at (%d,%d)\n", location.x, location.y);
			int id = startLake(location, next);
			Logger::Instance().Log ("lake %d is fed from (%d,%d)\n", id, next.x, next.y);
			lakes[id].displayLake();
			printHeightmap ();
			if (! Executive::Instance().onMap(next))
			{
				Logger::Instance().Log ("lake flows off of the map\n");
				continue;
			}
		}

		if (! Executive::Instance().onMap(next))
		{
			Logger::Instance().Log ("river flows off of the map\n");
			continue;
		}

		int nextIndex = indexOf(next.x, next.y);

		// at this point we have a flow gradient
		// flow = forest[index].getOutflow();
		//flow = 1;

		// change the river course to follow the highest flow between nodes
		if (flow > forest[nextIndex].largestFlow)
		{
			forest[nextIndex].largestFlow = flow;
			forest[nextIndex].upstream = location;
			forest[nextIndex].direction_into = Executive::Instance().directionFrom(location, next);
			forest[index].direction_out = forest[nextIndex].direction_into;
		}

		forest[nextIndex].addInflow(flow);
//		Logger::Instance().Log ("adding %d to flow into (%d,%d)\n", flow, next.x, next.y);

		forest[index].downstream = next;

	}

	int total = 0;
	int ocean = 0;

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

			int index = indexOf(i, j);
			flow = forest[index].getOutflow();
			Point downstream = forest[index].downstream;

			int nextFlow = flowAt(downstream);
			Logger::Instance().Log ("(%d,%d) sending %d water to (%d,%d), which has %d\n", i, j, flow, 
				downstream.x, downstream.y, nextFlow);
			total += flow;
		}
	}

	// total can be larger than the number of vertices, since a single unit of water counts in each point downstream
	Logger::Instance().Log ("moved a total of %d water, %d water areas did not contribute \n", total, ocean);

#if 0
	Point location(current_x, current_y);
	int currentIndex = indexOf (current_x, current_y);

	int height = Executive::Instance().getHeight(location);
	Logger::Instance().Log ("point %d,%d has height %d\n", current_x, current_y, height);

	int slope;
	Point p;

	// Turn this into a local member which finds the lowest new point
	Executive::Instance().findGradient(GRAD_MINIMUM,location, p, slope);

	int h2 = Executive::Instance().getHeight(p);
	Logger::Instance().Log ("  lowest neighbor is (%d,%d), height = %d, slope %d\n", p.x, p.y, h2, slope);

	// the amount of water flowing out of this point, minimum = 1 (at mountain peaks)
	int outflow = forest[currentIndex].getOutflow();

	forest[currentIndex].setGradient (slope);
	forest[currentIndex].setChild (p);

	int childIndex = indexOf(p);
	forest[childIndex].unsetRoot();

	// move to the next point on the map
	current_x++;
	if (current_x > params.x_size - 1)
	{
		current_x = 0;
		current_y++;
	}
#endif
}

// find the downstream point of "src"
// returns true if a point was found (and downstream is updated)
// otherwise returns false if "src" flows into the ocean or off the map

bool ErosionAgent::downstreamPoint (Point& src, Point& downstream)
{
	int src_Index = indexOf(src);
	Point p = forest[src_Index].downstream;

	// a quick and dirty check for point validity
	if ((p.x != -1))
	{
		downstream = p;
		return true;
	}

	Logger::Instance().Log ("point (%d,%d) directs flow to (%d,%d) which is off the map\n",
		src.x, src.y, p.x, p.y);
	return false;
}

void ErosionAgent::setDownstream(Point &p, Point &down)
{
	int index = indexOf(p);
	forest[index].downstream = down;
}

void ErosionAgent::setUpstream(Point& p, Point& up)
{
	int index = indexOf(p);
	forest[index].upstream = up;
}

void ErosionAgent::setLakeDownstream(int lake_id, Point& down)
{
	Point p;

	lakes[lake_id].Reset_Iterator();
	while (lakes[lake_id].Iterate_Next(p))
	{
		setDownstream (p, down);
	}
}

void ErosionAgent::setLakeUpstream(int lake_id, Point& down)
{
	Point p;

	lakes[lake_id].Reset_Iterator();
	while (lakes[lake_id].Iterate_Next(p))
	{
		setUpstream (p, down);
	}
}

// lake:
// raise a point to that of the lowest adjacent point
// we want this adjacent point to begin moving water elsewhere


int ErosionAgent::startLake (Point& p, Point& inflow)
{
	Point lowest;
	int height = Executive::Instance().getHeight(p);

	int id = lakes.size();
	Logger::Instance().Log ("starting lake %d at (%d,%d) height %d\n", id, p.x, p.y, height);

	Lake newLake(id);
	lakes.push_back(newLake);
	Logger::Instance().Log ("%d lakes defined\n", lakes.size());

	//printHeightmap ();

	Point downstream;

	lowest = p;
	while (1)
	{
		Logger::Instance().Log ("add point %d,%d to lake set\n", lowest.x, lowest.y);

		// pass along the flow into p
		Point p;
		int flow;

		if (highestInflowNeighbor(id, lowest, p))
		{
			int flow = flowAt(p);
			Logger::Instance().Log ("highest inflow into (%d,%d) is %d through %d,%d\n",
				lowest.x, lowest.y, flow, p.x, p.y);
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
				Logger::Instance().Log ("lake %d empties off the map\n", id);
				inflow = downstream;
				return id;
			}

			Logger::Instance().Log ("possible overflow point at (%d,%d)\n", downstream.x, downstream.y);
			// by definition the point "downstream" is not currently in the lake.  However we need to
			// check if it is currently flowing into a lake point (and therefore needs to be added)

			Point nextPoint;		// where the downstream point is flowing
			if (downstreamPoint(downstream, nextPoint))
			{
				Logger::Instance().Log ("lake %d considers overflowing towards(%d,%d)\n", id, nextPoint.x,
					nextPoint.y);
				int height = Executive::Instance().getHeight(downstream);

				// a point was found
				if (! lakes[id].inLake(nextPoint))
				{
					Logger::Instance().Log ("lake %d empties out of %d,%d to %d,%d\n",
						id, downstream.x, downstream.y, nextPoint.x, nextPoint.y);
					lakes[id].setOutflowPoint(nextPoint);
					if (! lakes[id].getInflowPoint(inflow))
					{
						Logger::Instance().Log ("unknown inflow point for this lake\n");
					}
					
					lakes[id].raiseLake(height);

					setLakeDownstream (id, nextPoint);
					setLakeUpstream (id, inflow);

					return id;
				}
				else
				{
					Logger::Instance().Log ("this point appears to be inside the lake\n");
				}

				// the downstream point also empties into the lake
				lowest = downstream;
				Logger::Instance().Log ("point %d,%d empties into the lake\n", lowest.x, lowest.y);
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
				setLakeDownstream (id, downstream);
				setLakeUpstream (id, inflow);
				Logger::Instance().Log ("(%d,%d) flows off the map\n", downstream.x, downstream.y);
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

bool ErosionAgent::highestInflowNeighbor(int lake_id, Point& src, Point& neighbor)
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

void ErosionAgent::printHeightmap ()
{
	for (int j = 0; j < 10; j++)
	{
		for (int i = 0; i < 10; i++)
		{
			Point p(i,j);
			int height = Executive::Instance().getHeight(p);

			Logger::Instance().Log ("%05d ", height);
		}
		Logger::Instance().Log ("\n");
	}
}


void ErosionAgent::flowWater ()
{
	Params& params = Params::Instance();

	sortedForest = forest;
	std::sort (sortedForest.begin(), sortedForest.end(), flowCompare);

	PointSet visited;

	// since a river ends at a high flow point, any downstream points would have higher
	std::vector<TreeNode>::iterator iter;
	for (iter = sortedForest.begin(); iter != sortedForest.end(); ++iter)
	{
		TreeNode node = *iter;
		int index = node.getID();
		Point p;
		
		pointOf(index, p);
		int flow = node.getOutflow();

		if (visited.in_set(p.x, p.y))
		{
			continue;
		}

		visited.insert(p);
	//	Logger::Instance().Log ("insert (%d,%d) into visited set\n", p.x, p.y);

		if (flow > minimum_flow)
		{
			Logger::Instance().Log ("river ends at (%d,%d) flow %d\n", p.x, p.y, flow);
			Point outflow = node.downstream;
			Logger::Instance().Log ("outflow to (%d,%d)\n", outflow.x, outflow.y);
		}
		else
		{
			continue;
		}

		while (flow > minimum_flow)
		{
			p = node.upstream;
			if (p.x == -1)
				break;
			Logger::Instance().Log ("flow at %d,%d is %d\n", p.x, p.y, flow);

			if (visited.in_set(p.x, p.y))
			{
				break;
			}

	//		Logger::Instance().Log ("visited set: ");
		//	visited.printSet();

			int id = indexOf(p);
			node = forest[id];
			flow = node.getOutflow();

			if (flow < minimum_flow)
				continue;

		//	Logger::Instance().Log ("inserting (%d,%d) into visited set\n", p.x, p.y);
			visited.insert (p);

			int width = (flow/2) + 1;
			width = min(width, 5);
			width = 5;
			previous = forest[id].upstream;
			prev_height = Executive::Instance().getHeight(previous);
			current_direction = forest[id].direction_out;;
			prev_dir = forest[id].direction_into;

			if (prev_dir < 0)
				continue;
			textureRiver (p, width);

	
		}
		Logger::Instance().Log ("river starts at (%d,%d)\n", p.x, p.y);
		Logger::Instance().Log ("========\n");
	}
#if 0
	for (int i = 0; i < params.x_size; i++)
	{
		for (int j = 0; j < params.y_size; j++)
		{
			Point p(i, j);
			int index = indexOf(i, j);

			if (!Executive::Instance().on_land(p))
			{
				continue;
			}

			int flow = forest[index].getOutflow();
			if (flow < 10)
				continue;

			int width = (flow/2) + 1;
			width = min(width, 10);
			previous = forest[index].upstream;
			prev_height = Executive::Instance().getHeight(previous);
			current_direction = forest[index].direction_out;;
			prev_dir = forest[index].direction_into;

			if (prev_dir < 0)
				continue;
			textureRiver (p, width);
		}
	}
#endif
}
// ===================================================================
// Move water downhill from node "id", and return the id of the new node
// or -1 if we hit the ocean.
// ===================================================================

int ErosionAgent::waterFlow(int id)
{
	Point lower;
	int childId;

	currentPath.insert(id);

	Point current;
	pointOf(id, current);

	if (! Executive::Instance().on_land(current))
	{
	//	Logger::Instance().Log ("stopping water flow at (%d,%d) since we reached water\n", current.x, current.y);
		return -1;
	}

	Point testpoint(1,2);
	int testHeight = Executive::Instance().getHeight(testpoint);
//	Logger::Instance().Log ("test point height (1,2) is %d\n", testHeight);

	// note: this does not use the spanning forest calculated earlier
	// find a lower unvisited point, or stop the water flow here
	if (! findLowestNotInPath (current, lower))
	{
		//Logger::Instance().Log ("no lower node found not in path, from (%d,%d)\n", current.x, current.y);
		//Logger::Instance().Log ("Path: ");
		//std::set<int>::iterator iter;

		//for (iter = currentPath.begin(); iter != currentPath.end(); ++iter)
		//{
		//	Logger::Instance().Log ("%d, ", *iter);
		//}
		//Logger::Instance().Log ("\n");

		return -1;
	}

	//forest[id].getChild(lower);

	childId = indexOf(lower);

	int currentHeight = Executive::Instance().getHeight(current);
	int h2 = Executive::Instance().getHeight(lower);

	// propagate water flow to lower point
	//int inflow = forest[currentRoot].getOutflow();
	int inflow = 1;
	forest[childId].addInflow(inflow);
	int childRate = forest[childId].getOutflow();

	if ((currentHeight > h2) && (Executive::Instance().on_land(lower)))
	{
		//Logger::Instance().Log ("moving %d water from node %d (%d,%d) to node %d (%d,%d), child moving %d water\n", inflow, id, 
		//	current.x, current.y, childId, lower.x, lower.y, childRate);
		return childId;
	}
	else
	{
		//Logger::Instance().Log ("flow ends at child %d (%d,%d), current height = %d, best child %d\n",
		//	childId, lower.x, lower.y, currentHeight, h2);
		//if (! Executive::Instance().on_land(lower))
		//{
		//	Logger::Instance().Log ("lower point is not on land\n");
		//}
		return -1;
	}
}

int ErosionAgent::flowAt (Point& p)
{
	if (! Executive::Instance().onMap(p))
	{
		Logger::Instance().Log ("flow check for point %d,%d which is off of the map\n", p.x, p.y);
		return 0;
	} 

	int index = indexOf(p);
	return (forest[index].getOutflow());
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

bool ErosionAgent::findLowestNotInPath (Point& current, Point& next)
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

#if 0
		// skip over points which are not on the map
		if (! Executive::Instance().onMap(neighbor))
		{
	//		Logger::Instance().Log ("ignoring point (%d,%d) since it is off map\n", neighbor.x, neighbor.y);
			continue;
		}
#endif
		// skip over neighbors which we have visited
		if (inPath(neighbor))
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

void ErosionAgent::performTextureWalk ()
{
	Params& params = Params::Instance();
	Point p;

	for (forest_iterator = sortedForest.begin(); forest_iterator != sortedForest.end(); ++forest_iterator)
	{
		TreeNode node = *forest_iterator;
		int flow = node.getOutflow();

		if (flow < 20)
			continue;

		Point current;
		pointOf(id, current);

		Point uphill;
		int slope;
		Executive::Instance().findGradient(GRAD_MAXIMUM, current, uphill, slope);
		int prev_dir = Executive::Instance().directionFrom(uphill, current);
	}

#if 0
	for (int i = 0; i < params.x_size; i++)
		for (int j = 0; j < params.y_size; j++)
		{
			Point p(i, j);
			int flow = flowAt(p);
			int width = (flow/2) + 1;
			previous = p;
			prev_height = Executive::Instance().getHeight(p);
			current_direction = 1;
			prev_dir = 1;
			textureRiver (p, width);
		}
#endif
}

void ErosionAgent::textureRiver (Point& location, int width)
{
	//Logger::Instance().Log ("building downstream at (%d,%d)\n", location.x, location.y);

	int height = Executive::Instance().getHeight(location);
	height = min (height, prev_height);
	height = max (0, height - 5);
	prev_height = height;
	SetHeightOp altitudeOp(height);
	TextureOp textureOp(TEXTURE_LAVA);
	FixPointOp fixpointOp;

	altitudeOp.setOverride(true);
	fixpointOp.setOverride(true);
	textureOp.setOverride(true);

	//Logger::Instance().Log ("preparing altitudeOp to set height to %d\n", height);
	WidenerOp widenerOp(altitudeOp, width, current_direction, false);
	widenerOp.setPrevious(previous, prev_dir);
	Executive::Instance().operatePoint(location, widenerOp);

	//Logger::Instance().Log ("texturing riverbed\n");

	int random_width = width + rand() % 3 - 1;

	WidenerOp textureRiver(textureOp, random_width, current_direction, false);
	textureRiver.setPrevious(previous, prev_dir);
	Executive::Instance().operatePoint(location, textureRiver);

	WidenerOp fixRiver(fixpointOp, random_width, current_direction, false);
	fixRiver.setPrevious(previous, prev_dir);
	Executive::Instance().operatePoint(location, fixRiver);

	// add to river set
//	SetInsertOp riverInsert(&points);
//	WidenerOp addToRiver(riverInsert, random_width, current_direction, false);
//	addToRiver.setPrevious(previous, prev_dir);
//	Executive::Instance().operatePoint(location, addToRiver);
}

// ===================================================================
// Determine if a node has already been visited on this walk
// ===================================================================

bool ErosionAgent::inPath (Point& p)
{
	int index = indexOf(p);
	return inPath (index);
}

bool ErosionAgent::inPath (int id)
{
	if (currentPath.find(id) != currentPath.end())
	{
		return true;
	}
	else
	{
		return false;
	}
}

TreeNode::TreeNode ()
{
	inflow = 1;
	gradient = 0;

	//child = Point(-1,-1);
	upstream = Point(-1, -1);
	downstream = Point(-1, -1);
	largestFlow = 0;
}

void TreeNode::pointOf(int id, Point& p)
{
	Params& params = Params::Instance();

	p.x = id % params.x_size;
	p.y = id / params.x_size;
}

int TreeNode::getHeight()
{
	Point p;
	pointOf(id, p);
	return Executive::Instance().getHeight(p);
}

// ===================================================================
// Less-than operator, which compares heights
//
// This allows us to sort the forest by height
// ===================================================================

bool operator < (TreeNode& left, TreeNode& right)
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

bool flowCompare (TreeNode& left, TreeNode& right)
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