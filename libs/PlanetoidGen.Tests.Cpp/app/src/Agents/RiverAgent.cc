#include "RiverAgent.h"
#include "Widener.h"
#include "executive.h"
#include "logger.h"
#include <sstream>
#include <math.h>
#include "params.h"

using namespace std;

int RiverAgent::count = 0;

RiverAgent::RiverAgent(int _tokens)
{
	type = RIVER_AGENT;
	tokens = _tokens;

	count++;
	id = count;

	ostringstream buf;
	buf << "River Agent #" << id;
	name = buf.str();

	runnable = false;
	initializing = true;

	width = 5;
	base_direction = rand() % 8;
	current_direction = base_direction;


}

// ===================================================================
// Execute one timestep
// ===================================================================
bool RiverAgent::Execute ()
{
	Params& params = Params::Instance();

	if (tokens == 0)
	{
		return false;
	}

	int dist;

	while (1)
	{
		for (int i = 0; i < 20; i++)
		{
			if (! findSuitableShorePoint (startPoint))
			{
				Logger::Instance().Log ("unable to find shoreline point\n");
				exit (1);
			}

			if (! findSuitableMountainPoint (endPoint))
			{
				Logger::Instance().Log ("unable to find mountain point\n");
				exit(1);
			}


			float d = (float) Executive::Instance().distanceSq(startPoint, endPoint);

			dist = (int) sqrt(d);
			Logger::Instance().Log ("river length = %d, sqr dist %f\n", dist, d);
			if (dist > params.min_river_length)
			{
				break;
			}
		}

		PointList path;
		calculatePath (startPoint, endPoint, path);

		// throw away a couple of points in the belief these are borderline

		for (int i = 0; i < params.river_backoff; i++)
		{
			path.pop_back();
		}

		if (path.size() < (unsigned int) params.min_river_length)
		{
			Logger::Instance().Log ("reject candidate river with %d points\n\n", path.size());
			continue;
		}

		// saveMergePoints (path);

		endPoint = path.back();
		path.pop_back();

		Point p = path.back();
		int height = Executive::Instance().getHeight(p);
		height = max(height - params.river_initialdrop, 0);

		buildRiverSegment (path);

		break;
	}

	return false;
}

void RiverAgent::buildLake (Point& p, int height)
{
	Logger::Instance().Log ("lake height will be %d\n", height);

	PointSet blob;
	PointSet border;

	blob.insert(p);
	border.insert(p);

	// build a blob
	for (int i = 0; i < 50; i++)
	{
		Point src;
		if (border.random_member(src))
		{
			for (int dir = 0; dir < 8; dir++)
			{
				Point adj;
				Executive::Instance().StepDir(src, adj, dir);
				if (blob.in_set(adj))
					continue;
				Logger::Instance().Log("add %d,%d to lake blob\n", adj.x, adj.y);
				blob.insert(adj);
				border.insert (adj);

				if (surroundingPtsInSet (src, blob))
				{
					border.remove(src);
				}
				if (surroundingPtsInSet (adj, blob))
				{
					border.remove(adj);
				}
				break;
			}
		}
	}

	Point blobPoint;
	blob.Reset_Iterator();

	SetHeightOp altitudeOp(height);
	TextureOp textureOp(TEXTURE_LAVA);
	SmootherOp smoothOp;
	FixPointOp fixpointOp;

	altitudeOp.setOverride(true);
	smoothOp.setOverride(true);
	textureOp.setOverride(true);

	while (blob.Iterate_Next(blobPoint))
	{
		Executive::Instance().operatePoint(blobPoint, altitudeOp);
	}

	blob.Reset_Iterator();
	while (blob.Iterate_Next(blobPoint))
	{
		Executive::Instance().operateArea(blobPoint, smoothOp);

	}

	blob.Reset_Iterator();
	while (blob.Iterate_Next(blobPoint))
	{
		Executive::Instance().operatePoint(blobPoint, textureOp);
		Executive::Instance().operatePoint(blobPoint, fixpointOp);
	}
}

// check if all surrounding points are in a set
bool RiverAgent::surroundingPtsInSet (Point& center, PointSet& pointset)
{
	Point p;

	for (int dir = 0; dir < 8; dir++)
	{
		Executive::Instance().StepDir(center, p, dir);
		if (! pointset.in_set(p))
		{
			return false;
		}
	}

	return true;
}

void RiverAgent::calculatePath (Point& startPoint, Point& endPoint, PointList& path)
{
	Params& params = Params::Instance();
	previous = startPoint;

	current_direction = Executive::Instance().directionFrom(startPoint, endPoint);
	base_direction = current_direction;
	prev_dir = current_direction;
	location = startPoint;

	path.push_back (location);
	Executive::Instance().StepDir(location, location, current_direction);

	//Logger::Instance().Log ("calculate path from %d,%d to %d,%d, base direction is %d\n",
	//	startPoint.x, startPoint.y, endPoint.x, endPoint.y, current_direction);

	while (location != endPoint)
	{
		if (! Executive::Instance().onMap(location))
		{
			//Logger::Instance().Log ("path build stopped due to running off the map\n");
			path.clear();
			break;
		}


		int height = Executive::Instance().getHeight(location);
		if (height > params.river_heightlimit)
		{
			//Logger::Instance().Log ("path build stopped due to excess height %d\n", height);
			break;
		}

		// don't allow coast to coast rivers
		if (Executive::Instance().inOcean(location))
		{
			//Logger::Instance().Log ("path stopped at coastline (%d,%d)\n", location.x, location.y);
			path.clear();
			break;
		}

		int newDir = bestDirection (location);

		Executive::Instance().StepDir(location, location, newDir);
		current_direction = newDir;
		path.push_back (location);

	//	Logger::Instance().Log ("%s: moving upstream to (%d,%d)\n", name.c_str(), location.x, location.y);
	}
}

// ===================================================================
// Advance the agent's path.
//
// Move the agent forward, optionally zig-zag about base_direction or
// change the width.
// ===================================================================

void RiverAgent::advancePath ()
{
	previous = location;
	prev_dir = current_direction;

	int newDir = bestDirection (location);

	Executive::Instance().StepDir(location, location, newDir);
	current_direction = newDir;
	path.push_back (location);

//	Logger::Instance().Log ("%s: moving upstream to (%d,%d)\n", name.c_str(), location.x, location.y);
}

void RiverAgent::buildRiverSegment(PointList& path)
{
	Params& params = Params::Instance();
	int width = params.river_initial_width;
	
	Point start = path.back();
	prev_height = Executive::Instance().getHeight(start);

	while (! path.empty())
	{
		Point p = path.back();
		path.pop_back();

		if (path.size() % params.river_widen_freq == (params.river_widen_freq - 1))
		{
			width++;
			//Logger::Instance().Log ("increase width to %d\n", width);
		}

		buildRiver (p, width);
	}
}

void RiverAgent::buildRiver (Point& location, int width)
{
	Params& params = Params::Instance();
	//Logger::Instance().Log ("building downstream at (%d,%d)\n", location.x, location.y);

	int height = Executive::Instance().getHeight(location);
	height = min (height, prev_height);
	height = max (0, height - params.river_slope);
	prev_height = height;
	SetHeightOp altitudeOp(height);
	TextureOp textureOp(TEXTURE_LAVA);
	FixPointOp fixpointOp;

	altitudeOp.setOverride(true);
	fixpointOp.setOverride(true);
	textureOp.setOverride(true);

	//int random_width = width + rand() % 3 - 1;
	int random_width = width;

	//Logger::Instance().Log ("preparing altitudeOp to set height to %d\n", height);

	//Logger::Instance().Log ("widening riverbed at (%d,%d), previous point (%d,%d), dir = %d, prev_dir = %d\n",
	//	location.x, location.y, previous.x, previous.y, current_direction, prev_dir);

	WidenerOp widenerOp(altitudeOp, random_width, current_direction, false);
	widenerOp.setPrevious(previous, prev_dir);
	Executive::Instance().operatePoint(location, widenerOp);

	//Logger::Instance().Log ("texturing riverbed\n");

	WidenerOp textureRiver(textureOp, random_width, current_direction, false);
	textureRiver.setPrevious(previous, prev_dir);
	Executive::Instance().operatePoint(location, textureRiver);

	WidenerOp fixRiver(fixpointOp, random_width, current_direction, false);
	fixRiver.setPrevious(previous, prev_dir);
	Executive::Instance().operatePoint(location, fixRiver);

	// add to river set
	SetInsertOp riverInsert(&points);
	WidenerOp addToRiver(riverInsert, random_width, current_direction, false);
	addToRiver.setPrevious(previous, prev_dir);
	Executive::Instance().operatePoint(location, addToRiver);

	previous = location;
	prev_dir = current_direction;
}

void RiverAgent::saveMergePoints(PointList& path)
{
	int length = path.size();
	int tribs = length / 100;

	//Logger::Instance().Log ("%d points in river, %d tributaries\n", length, tribs);

	mergePoints.clear();

	for (int i = 0; i < tribs; i++)
	{
		int index = rand() % length;
		mergePoints.push_back(path[index]);
		Logger::Instance().Log ("merge tributary at %d,%d\n", mergePoints[i].x, mergePoints[i].y);
	}
}

// ===================================================================
// Zig-zag about the base direction
//
// Stays within a 90-degree cone
// ===================================================================

void RiverAgent::changeDirection()
{
	if (current_direction == base_direction)
	{
		if (rand() % 2 == 1)
		{
			++current_direction;
		}
		else
		{
			--current_direction;
		}
	}
	else if (current_direction == ((base_direction + 1) % 8))
	{
		--current_direction;
	}
	else
	{
		++current_direction;
	}

	if (current_direction == 8)
	{
		current_direction = 0;
	}
	if (current_direction == -1)
	{
		current_direction = 7;
	}
}

// ===================================================================
// Check directions in our travel cone, and look for a good delta
//
// For now, a good delta is the lowest point ahead of us.
// ===================================================================

int RiverAgent::bestDirection (Point& current)
{
	Point p;
	int h1 = Executive::Instance().getHeight(current);
	int lowest_ahead = 100000;
	int highest_ahead = 0;
	int best_dir = -1;

	int best_distance = 100000;

#if 1
	for (int i = 0; i < 3; i++)
	{
		int dir_offset = 8 + i - 1;
		int dir = (base_direction + dir_offset) % 8;

		Executive::Instance().StepDir(current, p, dir);

		int h2 = Executive::Instance().getHeight(p);

		if (h2 > highest_ahead)
		{
			highest_ahead = h2;
			best_dir = dir;
	//	Logger::Instance().Log ("change direction to %d, base is %d, offset = %d\n", best_dir, base_direction, dir_offset);
		}
	}
#endif

	if (best_dir == -1)
	{
		return current_direction;
	}


	return best_dir;
}

bool RiverAgent::findSuitableShorePoint (Point& p)
{
	Params& params = Params::Instance();
	Point shore;

	if (! Executive::Instance().random_boundary(shore, params.river_max_shore))
	{
		return false;
	}

	for (int dir = 0; dir < 8; dir++)
	{
		Point water;
		Executive::Instance().StepDir(shore, water, dir);
		if (Executive::Instance().inOcean(water))
		{
			p = water;
			return true;
		}
	}

	return false;
}

bool RiverAgent::findSuitableMountainPoint(Point& p)
{
	Params& params = Params::Instance();
	Point mountain;

	for (int i = 0; i < 1000; i++)
	{
		if (Executive::Instance().random_mountain(mountain, params.river_min_mountain))
		{
			if (Executive::Instance().distanceToCoastline(mountain) > (params.river_mountain_coast_dist * params.river_mountain_coast_dist))
			{
				p = mountain;
				int height = Executive::Instance().getHeight(mountain);
				//Logger::Instance().Log ("returning mountain point (%d,%d) at elevation %d\n",
				//	mountain.x, mountain.y, height);
				return true;
			}
		}
	}

	return false;
}