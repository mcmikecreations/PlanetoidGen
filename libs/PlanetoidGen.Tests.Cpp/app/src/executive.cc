#include "executive.h"
#include "logger.h"
#include "agent.h"
#include "params.h"
#include <fstream>
#include <sstream>
#include <time.h>
#include <deque>
#include <limits>

#include "MountainAgent.h"

using namespace std;

Executive::Executive ()
{
	Params& params = Params::Instance();

	mask = NULL;

	map = new Heightmap(params.x_size, params.y_size);
	map -> SetMode (rgba_8);
	map -> SetName("./split/mapgen3.");
	map -> SetFormat(params.format);

	texture = new Index(params.x_size, params.y_size);
	texture -> SetName("./split/mapgen3.Index.");
	texture -> SetFormat(params.format);

	// There are currently 16 textures in the atlas
	atlas_size = 16;

	numMountainAgents = 0;
	for (int i = 0; i < params.x_size; i++)
	{
		for (int j = 0; j < params.y_size; j++)
		{
			texture->SetPrimary(i, j, indexOf(1), 255);
			texture->SetSecondary(i, j, indexOf(1), 255);
		}
	}

#if 0
	Point p(3,144);
	watchPoint(p);
#endif
}

std::unique_ptr<Executive> Executive::_instance;

Executive& Executive::Instance()
{
	if (_instance.get() == NULL)
	{
		_instance.reset (new Executive);
	}

	return *_instance;
}

// ===================================================================
// return the current time as a printable string
// ===================================================================
string Executive::currentTime()
{
	time_t now = time(NULL);
	char tbuf[80] = { 0 };
#if defined _linux or defined __unix__ or defined __linux__ or defined __unix or defined __linux
	ctime_r(&now, tbuf);
#else
    ctime_s(tbuf, sizeof(tbuf), &now);
#endif
	return string(tbuf);
}


// ===================================================================
// Return the coordinates of the neighbor to (x,y) in the specified direction
//
// If the neighboring point is on the map, updates (neighbor_x, neighbor_y) and returns true.
// Otherwise, returns false.
// ===================================================================

bool Executive::neighbor (Point& seed, Point& neighbor, int direction)
{
	return map->neighbor(seed, neighbor, direction);
}

// ===================================================================
// Return a random neighboring point
// ===================================================================
bool Executive::random_neighbor (Point& src, Point& neighbor)
{
	return map->random_neighbor(src, neighbor);
}

// ===================================================================
// An alternative to neighbor, lifted from Action
// ===================================================================
bool Executive::StepDir (Point& src, Point& dst, int direction, int delta)
{
	return map->StepDir(src, dst, direction, delta);
}

// ===================================================================
// return the maximum altitude on the heightmap
// ===================================================================

unsigned long Executive::maxAltitude ()
{
	return map->Max();
}

unsigned long Executive::minAltitude ()
{
	return map->Min();
}

// ===================================================================
// Return a random point on the landmass (elevation > 0)
// ===================================================================

bool Executive::random_land(Point &point)
{
	if (mask == NULL)
	{
		Logger::Instance().Log ("Executive::random_land: no mask assigned, aborting run\n");
		exit (-1);
	}

	return mask->randomPointOnMask(point);
}

// ===================================================================
// Return a random point on the land/water boundary
// ===================================================================
bool Executive::random_boundary(Point& point, int maxAltitude)
{
	if (maxAltitude == 0)
		return coastline.random_member(point);

	// empty coastline
	if (coastline.size() == 0)
	{
		return false;
	}

	int pos = rand() % coastline.size();
	if (! coastline.position(pos))
	{
		Logger::Instance().Log ("unable to position coastline iterator at %d\n", pos);
		exit (1);
	}

	Point p;

	while (coastline.Iterate_Next(p))
	{
		int height = getHeight(p);
		if (height < maxAltitude)
		{
			point = p;
			return true;
		}
	}

	return false;
}

// ===================================================================
// Return a random point on a mountain
// ===================================================================
bool Executive::random_mountain(Point& point, int minAltitude)
{
	MountainAgent *agent = randomMountainAgent();

	if (agent == NULL)
	{
		return false;
	}

	return agent->randomPoint(point);
}

MountainAgent *
Executive::randomMountainAgent ()
{
	if (mountainAgents.size() == 0)
	{
		Logger::Instance().Log ("random_mountain: no mountain agents\n");
		return nullptr;
	}

	int pos = rand() % mountainAgents.size();

	AgentSet::iterator i = mountainAgents.begin();
	std::advance(i, pos);

	MountainAgent *agent = (MountainAgent*) *i;
	return agent;
}

// ===================================================================
// Determine if a point is on land (is on the map, and is set in the land mask)
// ===================================================================

bool Executive::on_land (Point& point)
{
	if (mask == NULL)
	{
		Logger::Instance().Log ("Executive::on_land: no mask assigned, aborting run\n");
		exit (-1);
	}

	// check range of point
	if (! onMap(point))
	{
		return false;
	}

	// check if the point is on the mask
	return mask->is_set(point.x, point.y);
}

// ===================================================================
// Determine if a point is on the shoreline
// ===================================================================

bool Executive::on_shore(Point &point)
{
	if (mask == NULL)
	{
		Logger::Instance().Log ("Executive::on_shore: no mask assigned, aborting run\n");
		exit (-1);
	}

	// return mask->on_boundary(point.x, point.y);
	return coastline.in_set(point.x, point.y);
}

// ===================================================================
// return the opposite of a given direction
// ===================================================================
int Executive::oppositeDirection(int direction)
{
	return (direction + 4) % 8;
}

void Executive::identifyCoastline ()
{
	Point p(0,0);

	while (on_land(p))
	{
		p.x++;
	}

	if (! onMap(p))
	{
		Logger::Instance().Log ("cannot place initial water point\n");
		exit (1);
	}

	deque<Point> openVertices;
	openVertices.push_back(p);

	ocean.insert(p);

	while (! openVertices.empty())
	{
		Point p = openVertices.front();
		openVertices.pop_front();
		ocean.insert(p);

		for (int dir = 0; dir < 8; dir++)
		{
			Point adjacent;

			StepDir(p, adjacent, dir);

			// don't visit points which are off of the map
			if (! onMap(adjacent))
			{
				continue;
			}

			// don't visit points more than once
			if (ocean.in_set(adjacent))
			{
				continue;
			}

			if (on_land(adjacent))
			{
				// adjacent to an open water
				coastline.insert(adjacent);
				//ocean.insert(adjacent);
			}
			else
			{
				// open ocean
				ocean.insert(adjacent);
				openVertices.push_back(adjacent);
			}
		}
	}

#if LOG_COASTLINE_POINTS
	coastline.Reset_Iterator();
	int count = 0;
	while (coastline.Iterate_Next(p))
	{
		Logger::Instance().Log (" (%d,%d) ", p.x, p.y);
		if (count++ % 10 == 9)
		{
			Logger::Instance().Log ("\n");
		}
	}

	Logger::Instance().Log ("\n");
#endif
}

// mask boundary points are:
//   1) on land
//   2) have at least one adjacent point which is not in the mask
//   3) has not been visited yet

bool Executive::onMaskBoundary(Point& p)
{
	if (! on_land(p))
	{
		return false;
	}

	if (coastline.in_set(p))
	{
		return false;
	}

	Point next;
	for (int dir = 0; dir < 8; dir++)
	{
		StepDir(p, next, dir);

		if (coastline.in_set(next))
		{
			continue;
		}

		if (! mask->is_set(next.x, next.y))
		{
			return true;
		}
	}

	return false;
}

void Executive::addAgent(Agent *a)
{
	AgentType type = a->getType();

	// defer running river agents
	switch (type)
	{
	case EROSION_AGENT:
	case RIVER_AGENT:
		riverAgents.insert(a);
		break;
	case MOUNTAIN_AGENT:
		mountainAgents.insert(a);
		runnable.insert(a);
		break;
	default:
		runnable.insert(a);
		break;
	}
}

// ===================================================================
// record a completed mountain
//
// When a MountainAgent completes, it will call this method to record itself.
// When the last mountain has completed, the RiverAgents can start
// ===================================================================
void Executive::agentCompleted (Agent *a)
{
	AgentType type = a->getType();

	a->setRunnable(false);

	// Logger::Instance().Log ("%s completing\n", a->getName().c_str());
#if 0
	switch (type)
	{
	case MOUNTAIN_AGENT:
		if (! anyRunnable (mountainAgents))
		{
			// the last mountain agent has completed
			Logger::Instance().Log ("no runnable MountainAgents, starting RiverAgents\n");
			makeRunnable (riverAgents);
			return;
		}
		else
		{
			Logger::Instance().Log ("still runnable MountainAgents\n");
		}
	}
#endif
	static bool one_time = true;

	if (one_time && (runnable.size() == 0))
	{
		Logger::Instance().Log ("starting river agent\n");
		makeRunnable (riverAgents);
		one_time = false;
	}
}

// ===================================================================
// determine if any agents in a set are runnable
// ===================================================================
bool Executive::anyRunnable(AgentSet& agents)
{
	AgentSet::iterator iter;

	for (iter = agents.begin(); iter != agents.end(); ++iter)
	{
		Agent *a = *iter;

		if (a ->isRunnable())
		{
			Logger::Instance().Log ("%s is still runnable\n", a->getName().c_str());
			return true;
		}
	}

	return false;
}

// ===================================================================
// mark all agents in a set as runnable
// ===================================================================
void Executive::makeRunnable(AgentSet& agents)
{
	AgentSet::iterator iter;

	for (iter = agents.begin(); iter != agents.end(); ++iter)
	{
		Agent *a = *iter;

		a ->setRunnable(true);
		runnable.insert(a);
	}
}

// ===================================================================
// find an interior point near the coastline
// ===================================================================
bool Executive::findInterior (Point& point, int distance)
{
	Point p;
	int direction = rand() % 8;

	for (int i = 0; i < 8; i++)
	{
		StepDir(point, p, direction, distance);
		if (on_land(p) && !on_shore(p))
		{
			point.x = p.x;
			point.y = p.y;
			return true;
		}

		++direction %= 8;
	}

	return false;
}

void
Executive::printArea(Point &point)
{
	for (int j = -2; j < 4; j++)
	{
		for (int i = -2; i < 4; i++)
		{
			Point p(point.x + i, point.y + j);
			int height = getHeight(p);

			if (mask->on_boundary(p.x, p.y))
				Logger::Instance().Log ("+");
			else
				Logger::Instance().Log (".");
		}
		Logger::Instance().Log ("\n");
	}

	for (int j = -2; j < 4; j++)
	{
		for (int i = -2; i < 4; i++)
		{
			Point p(point.x + i, point.y + j);
			int height = getHeight(p);

			Logger::Instance().Log ("%05d ", height);
		}
		Logger::Instance().Log ("\n");
	}
}

// ===================================================================
// lookup a height value on the heightmap
// ===================================================================
unsigned long Executive::getHeight(Point& point)
{
	return map->Get(point.x, point.y);
}

// ===================================================================
// assign a new value to the heightmap
// ===================================================================
void Executive::setHeight(Point &p, unsigned long alt)
{
	if (! map->in_range(p.x, p.y))
	{
		return;
	}

	if (isWatched(p))
	{
		Logger::Instance().Log ("*** set height of %d,%d as %d\n", p.x, p.y, alt);
	}

	if (! isFixed(p))
	{
		map->Set(p.x, p.y, alt);
	}
	else if (isWatched(p))
	{
		Logger::Instance().Log ("point %d,%d is watched/fixed, cannot change altitude\n", p.x, p.y);
	}
}

// ===================================================================
// texture a point
// ===================================================================

void Executive::texturePoint (Point& p, int texture_id)
{
	if (! map->in_range(p.x, p.y))
	{
		return;
	}

	if (isWatched(p))
	{
		Logger::Instance().Log ("*** texture %d,%d as %d\n", p.x, p.y, texture_id);
	}

	if (! isFixed(p))
	{
		texture->SetPrimary(p.x, p.y, indexOf(texture_id), 255);
		texture->SetSecondary(p.x, p.y, indexOf(texture_id), 255);
	}
	else if (isWatched(p))
	{
		Logger::Instance().Log ("point %d,%d is fixed, cannot texture\n", p.x, p.y);
	}
}

// ===================================================================
// smooth the area around a point
// ===================================================================

void Executive::smoothPoint(Point &point)
{
//	if ((! isFixed(point)) && (on_land(point)))
	if (inOcean(point))
	{
		return;
	}

	if (! isFixed(point) && onMap(point))
	{
		unsigned long newAltitude = weightedAverageHeight (point);

		setHeight(point, newAltitude);
	}
}

// ===================================================================
// smooth the area around a point
// ===================================================================

void Executive::smoothArea(Point &point)
{
	Point current(point.x, point.y);

	smoothPoint (current);

	current.x = point.x + 1;
	current.y = point.y;
	smoothPoint (current);

	current.x = point.x - 1;
	current.y = point.y;
	smoothPoint (current);

	current.x = point.x;
	current.y = point.y - 1;
	smoothPoint (current);

	current.x = point.x;
	current.y = point.y + 1;
	smoothPoint (current);

	current.x = point.x + 2;
	current.y = point.y;
	smoothPoint (current);

	current.x = point.x - 2;
	current.y = point.y;
	smoothPoint (current);

	current.x = point.x;
	current.y = point.y + 2;
	smoothPoint (current);

	current.x = point.x;
	current.y = point.y - 2;
	smoothPoint (current);

	current.x = point.x + 3;
	current.y = point.y;
	smoothPoint (current);

	current.x = point.x - 3;
	current.y = point.y;
	smoothPoint (current);

	current.x = point.x;
	current.y = point.y + 3;
	smoothPoint (current);

	current.x = point.x;
	current.y = point.y - 3;
	smoothPoint (current);
}

// ===================================================================
// assign altitude around a point
// ===================================================================

void Executive::assignArea(Point &point, int altitude)
{
	Point current(point.x, point.y);

	setHeight (current, altitude);

	current.x = point.x + 1;
	current.y = point.y;
	setHeight (current, altitude);

	current.x = point.x - 1;
	current.y = point.y;
	setHeight (current, altitude);

	current.x = point.x;
	current.y = point.y - 1;
	setHeight (current, altitude);

	current.x = point.x;
	current.y = point.y + 1;
	setHeight (current, altitude);

	current.x = point.x + 2;
	current.y = point.y;
	setHeight (current, altitude);

	current.x = point.x - 2;
	current.y = point.y;
	setHeight (current, altitude);

	current.x = point.x;
	current.y = point.y + 2;
	setHeight (current, altitude);

	current.x = point.x;
	current.y = point.y - 2;
	setHeight (current, altitude);

	current.x = point.x + 3;
	current.y = point.y;
	setHeight (current, altitude);

	current.x = point.x - 3;
	current.y = point.y;
	setHeight (current, altitude);

	current.x = point.x;
	current.y = point.y + 3;
	setHeight (current, altitude);

	current.x = point.x;
	current.y = point.y - 3;
	setHeight (current, altitude);
}

void Executive::fixArea(Point &point)
{
	SetInsertOp op(&fixed_points);
	operateArea (point, op);
}

// ===================================================================
// calculate a weighted average of nearby points
// ===================================================================
unsigned long Executive::weightedAverageHeight (Point& point)
{
	long sum = 0;
	Point current(point.x, point.y);

	int x = point.x;
	int y = point.y;

	sum = 4 * getHeight (current);

	current.x = x + 1;
	sum += getHeight (current);

	current.x = x - 1;
	sum += getHeight (current);

	current.x = x + 2;
	sum += getHeight (current);

	current.x = x - 2;
	sum += getHeight (current);

	current.x = x;
	current.y = y + 1;
	sum += getHeight (current);

	current.y = y - 1;
	sum += getHeight (current);

	current.y = y + 2;
	sum += getHeight (current);

	current.y = y - 2;
	sum += getHeight (current);

	long average = sum / 12;
	return average;
}

// ===================================================================
// split the heightmap, and write out pages
// ===================================================================

void Executive::writeHeightmap()
{
	Params& params = Params::Instance();

	switch (params.format)
	{
		case FORMAT_PNG:
			map ->Scale(30000);
			map -> Write ("height_full.png");
			break;
		case FORMAT_JPG:

			map -> Write ("height_full.jpg");
			break;
		case FORMAT_TGA:
			map -> SetMode (grey_8);
			map ->Scale(255);
			map -> Write ("heightmap.tga");
			break;
	}
	Logger::Instance().Log ("writing map\n");
	generate_plsm_cfg ();
	map -> SplitImage (params.page_size);
	//texture->SetMode(lum_16);
	texture -> SplitImage(params.page_size);
}

// ===================================================================
// ===================================================================
void Executive::generate_plsm_cfg ()
{
	Params& params = Params::Instance();

	ofstream cfgFile;
	ostringstream filename;

	filename << "./split/mapgen3.cfg";
	cfgFile.open (filename.str().c_str());

	cfgFile << "# run-time configuration file for terrain data-set " << params.name
		<< std::endl << std::endl;

	cfgFile << "GroupName=PLSM2" << std::endl << std::endl;
	cfgFile << "LandScapeFileName=mapgen3" << std::endl;
	cfgFile << "FileSystem=LandScapeFileName" << std::endl;
	cfgFile << "LandScapeExtension=png" << std::endl << std::endl;

	cfgFile << "# height and width of the terrain in pages" << std::endl;
	cfgFile << "Width=" << params.num_x_pages << std::endl;
	cfgFile << "Height=" << params.num_y_pages << std::endl << std::endl;

	cfgFile << "Data2DFormat=HeightField" << std::endl;
	cfgFile << "NumTextureFormatSupported=1" << std::endl;
	cfgFile << "TextureFormatSupported0=IndexSplat" << std::endl;
	cfgFile << "TextureFormat=IndexSplat" << std::endl << std::endl;

	cfgFile << "VertexCompression=yes" << std::endl;
	cfgFile << "VertexProgramMorph=yes" << std::endl;
	cfgFile << "MaxPixelError=0" << std::endl;
	cfgFile << "VertexNormals=no" << std::endl;
	cfgFile << "Deformable=no" << std::endl;
	cfgFile << "VisibleRenderables=80" << std::endl;
	cfgFile << "MaxRenderLevel=5" << std::endl;
	cfgFile << "MaxRenderablesLoading=50" << std::endl;
	cfgFile << "MaxAdjacentPages=2" << std::endl;
	cfgFile << "MaxPreloadedPages=6" << std::endl;
	cfgFile << "MaxNumTiles=256" << std::endl;
	cfgFile << "IncrementRenderables=400" << std::endl;
	cfgFile << "IncrementTiles=400" << std::endl;

	// starting location
	int x = params.x_size / 4;
	int y = params.y_size / 4;
	int z = 100;

	cfgFile << "BaseCameraViewpoint.x=" << x << std::endl;
	cfgFile << "BaseCameraViewpoint.y=" << z << std::endl;
	cfgFile << "BaseCameraViewpoint.z=" << y << std::endl;

	cfgFile << "Baselookat.x=0.0f" << std::endl;
	cfgFile << "Baselookat.y=0.0f" << std::endl;
	cfgFile << "Baselookat.z=0.0f" << std::endl << std::endl;

	cfgFile << "ScaleX=9000" << std::endl;
	cfgFile << "ScaleY=2000" << std::endl;
	cfgFile << "ScaleZ=9000" << std::endl;
}

void Executive::operatePoint(Point &p, TerrainOp &op)
{
	op.Execute(p);
}

void Executive::operateArea(Point &point, TerrainOp &op)
{
	Point current(point.x, point.y);

	op.Execute(current);

	current.x = point.x + 1;
	current.y = point.y;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x - 1;
	current.y = point.y;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x;
	current.y = point.y - 1;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x;
	current.y = point.y + 1;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x + 2;
	current.y = point.y;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x - 2;
	current.y = point.y;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x;
	current.y = point.y + 2;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x;
	current.y = point.y - 2;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x + 3;
	current.y = point.y;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x - 3;
	current.y = point.y;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x;
	current.y = point.y + 3;
	if (onMap(current))
	{
		op.Execute(current);
	}

	current.x = point.x;
	current.y = point.y - 3;
	if (onMap(current))
	{
		op.Execute(current);
	}
}


// ===================================================================
// Perform any heightmap adjustments needed before agents run.
// ===================================================================
int Executive::indexOf (int slice)
{
	int slice_range = 256;

	int index = (slice * (slice_range/atlas_size));
	return index;
}

// ===================================================================
// Return the slope at a point
//
// Point slope is defined as the maximum height difference between a point and all
// adjacent points.  Since points are on a fixed grid, the height difference / grid size
// is the slope.
// ===================================================================

int Executive::maxGradient (Point& point)
{
	Point p;
	int max_grad = 0;

	int h1 = getHeight (point);

	for (int i = 0; i < 8; i++)
	{
		StepDir(point, p, i);
		int h2 = getHeight(p);

		int delta = abs(h1 - h2);
		if (delta > max_grad)
		{
			max_grad = delta;
		}
	}

	return max_grad;
}
// ===================================================================
// find a nearby point based on the gradient
//
// Different gradient types are supported for different purposes.
// GRAD_MAX_SLOPE:
//		the point of greatest difference, either positive or negative
// GRAD_MIN_SLOPE:
//		the point with the minimum slope (ie the flattest)
// GRAD_MAXIMUM:
//		the highest neighboring point
// GRAD_MINIMUM:
//		the lowest neighboring point
//
// Two parameters are altered upon completion:
//		point:		The neighboring point best meeting the criteria
//		slope:		The slope between 
// ===================================================================

void Executive::findGradient (GradientType type, Point& center, Point& point, int& slope)
{
	Point p;
	int grad_limit = 0;
	Point limit;

	switch (type)
	{
	case GRAD_MAX_SLOPE:
	case GRAD_MAXIMUM:
		grad_limit = 0;
		break;
	case GRAD_MIN_SLOPE:
	case GRAD_MINIMUM:
		grad_limit = 100000;
		break;
	}

	int h1 = getHeight (center);

	for (int i = 0; i < 8; i++)
	{
		StepDir(center, p, i);
		if (! onMap(p))
		{
			continue;
		}

		int h2 = getHeight(p);
		int delta;

		switch (type)
		{
		case GRAD_MAX_SLOPE:
			delta = abs(h1 - h2);
			if (delta > grad_limit)
			{
				grad_limit = delta;
				limit = p;
			}
			break;
		case GRAD_MIN_SLOPE:
			delta = abs(h1 - h2);
			if (delta < grad_limit)
			{
				grad_limit = delta;
				limit = p;
			}
			break;
		case GRAD_MINIMUM:
			if (h2 < grad_limit)
			{
				grad_limit = h2;
				limit = p;
			}
			break;
		case GRAD_MAXIMUM:
			if (h2 > grad_limit)
			{
				grad_limit = h2;
				limit = p;
			}
			break;
		default:
			Logger::Instance().Log ("Invalid gradient type passed to findGradient\n");
			exit (-1);
		}
	}

	int h2 = getHeight(limit);
	point = limit;
	slope = h1 - h2;
}

// ===================================================================
// determine the direction from one point to another
// ===================================================================

int  Executive::directionFrom (Point& src, Point& target)
{
	int dx = target.x - src.x;
	int dy = src.y - target.y;

	// same point
	if ((dx == 0) && (dy == 0))
	{
		Logger::Instance().Log ("directionFrom same point (%d,%d) to (%d,%d)\n", src.x, src.y, target.x, target.y);
		exit (1);
		return -1;
	}

//	Logger::Instance().Log ("direction from (%d,%d) to (%d,%d) has dx=%d, dy=%d\n", src.x, src.y, target.x, target.y, dx, dy);

	if ((dx == 0) && (dy > 0)) 	{ return DIR_UP; }
	if ((dx > 0) && (dy > 0)) 	{ return DIR_UR; }
	if ((dx > 0) && (dy == 0)) 	{ return DIR_RIGHT; }
	if ((dx > 0) && (dy < 0)) 	{ return DIR_LR; }
	if ((dx == 0) && (dy < 0)) 	{ return DIR_DOWN; }
	if ((dx < 0) && (dy < 0)) 	{ return DIR_LL; }
	if ((dx < 0) && (dy == 0)) 	{ return DIR_LEFT; }
	if ((dx < 0) && (dy > 0)) 	{ return DIR_UL; }

	Logger::Instance().Log ("unable to find direction from dx %d, dy %d\n", dx, dy);
	exit (1);
	return -1;
}

void Executive::shock_map (int num_points)
{
	Params& params = Params::Instance();

#if 0
	for (int i = 0; i < num_points; i++)
	{
		int x = rand() % (params.x_size / 2) + (params.x_size / 4);
		int y = rand() % (params.y_size / 2) + (params.y_size / 4);

		if (rand() % 2 == 1)
		{
			int height = rand() % (params.height_limit / 2) + 1000;
			randomBlob (x, y, height);
		}
		else
		{
			randomBlob (x, y, 0);
		}
	}
#endif

	for (int i = 0; i < 10; i++)
	{
		Point p;

		do
		{
			random_land(p);
		} while (distanceToCoastline(p) < 1000);

		int len = rand() % 500 + 400;
		for (int j = 0; j < len; j++)
		{
			setHeight (p, 14000);

			do
			{
				int dir = rand() % 8;
				StepDir (p, p, dir);
			} while (! on_land(p));
		}
	}
}

void Executive::randomBlob (int x, int y, int height)
{
	Params& params = Params::Instance();

	int area = params.x_size * params.y_size;
	int max_size = (area / 10) + 1;
	int steps = rand() % max_size;

	Point p(x, y);

	for (int i = 0; i < steps; i++)
	{
		Point next;

		setHeight(p, height);
		int dir = rand() % 8;

		do
		{
			StepDir(p, next, dir);
		} while (! onMap(next));
	}
}

// ===================================================================
// Perform any heightmap adjustments needed before agents run.
// ===================================================================

void Executive::Setup()
{
	Params& params = Params::Instance();

	Logger::Instance().Log ("starting coastline walk at %s\n", currentTime().c_str());
	identifyCoastline();
	Logger::Instance().Log ("ending coastline walk at %s\n", currentTime().c_str());

	Logger::Instance().Log ("starting randomization at %s\n", currentTime().c_str());

	// create some noise over the landmass
	map->randomize (params.noise_size);
	Logger::Instance().Log ("ending randomization at %s\n", currentTime().c_str());

//	int area = params.x_size * params.y_size;
//	shock_map (area / 2000);
}

void Executive::PostRun ()
{
	Params& params = Params::Instance();

	for (int i = 0; i < params.x_size; i++)
	{
		for (int j = 0; j < params.y_size; j++)
		{
			Point p(i,j);
			smoothPoint(p);
		}
	}
	unsigned int maxAlt = maxAltitude();
	unsigned int minAlt = minAltitude();
	unsigned int snowline = maxAlt - 5000;
	int dirtline = 2000;

	Logger::Instance().Log ("max alt = %d, snowline at %d, dirt begins at %d\n", maxAlt, snowline, dirtline);
	Logger::Instance().Log ("min alt = %d\n", minAlt);

	for (int i = 0; i < params.x_size; i++)
	{
		for (int j = 0; j < params.y_size; j++)
		{
			Point p(i,j);

			if (isFixed(p))
				continue;

			int height = getHeight(p);

			int grad = maxGradient(p);

			if (height > (int) snowline)
			{
				texturePoint(p, TEXTURE_SNOW);
			}
			else if (grad > 400)
			{
				texturePoint (p, TEXTURE_ROCK1);
			}
			else if (height < dirtline)
			{
				texturePoint(p, TEXTURE_DIRT3);		// kind of sandy
			}
			else
			{
				texturePoint(p, TEXTURE_GRASS2);
			}
		}
	}
}

// ===================================================================
// Return the squared distance between two points
// ===================================================================

int Executive::distanceSq(Point& p1, Point& p2)
{
	return (p1.x - p2.x)*(p1.x - p2.x) + (p1.y - p2.y)*(p1.y - p2.y);
}

int Executive::distanceToCoastline (Point& p)
{
	int minDistance = numeric_limits<int>::max();

	coastline.Reset_Iterator();
	Point shore;

	while (coastline.Iterate_Next(shore))
	{
		int dist = distanceSq(p, shore);
		if (dist < minDistance)
		{
			minDistance = dist;
		}
	}

	return minDistance;
}

// ===================================================================
// Simulate agent execution until no agents are runnable.
// ===================================================================

void Executive::Run()
{
	if (mask == NULL)
	{
		Logger::Instance().Log ("Executive::Run: no mask assigned, aborting run\n");
		return;
	}

	while (runnable.size() > 0)
	{
		AgentSet::iterator iter;
		AgentSet finished;

#if SEQUENTIAL_SCHEDULE
		for (iter = runnable.begin(); iter != runnable.end(); ++iter)
		{
			Agent *agent = *iter;

			if (! agent->Execute())
			{
				// schedule for deletion
				finished.insert(agent);
			}
		}
#endif
		iter = runnable.begin();
		int pos = rand() % runnable.size();
		std::advance(iter, pos);
		Agent *agent = *iter;

		int run = rand() % 4;
		bool result = true;

		for (int i = 0; i < run; i++)
		{
			result = agent->Execute();
		}

		if (! result)
		{
			runnable.erase (agent);
			agentCompleted(agent);
		}

#if SEQUENTIAL
		// clear out any agents which have finished running
		for (iter = finished.begin(); iter != finished.end(); ++iter)
		{
			Agent *agent = *iter;

			runnable.erase(agent);
			agentCompleted(agent);
			//delete agent;
		}

		finished.clear();
	}
#endif
	}
}
