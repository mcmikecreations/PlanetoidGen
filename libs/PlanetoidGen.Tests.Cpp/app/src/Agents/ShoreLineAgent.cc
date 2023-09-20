#include "ShoreLineAgent.h"
#include "executive.h"
#include "logger.h"
#include <sstream>
#include "TerrainOp.h"
#include "params.h"

using namespace std;

int ShoreLineAgent::count = 0;

ShoreLineAgent::ShoreLineAgent(int _tokens)
{
	type = SHORELINE_AGENT;
	tokens = _tokens;
	runnable = true;

	count++;
	id = count;

	ostringstream buf;
	buf << "ShoreLine Agent #" << id;
	name = buf.str();

	if (!Executive::Instance().random_boundary(location))
	{
		Logger::Instance().Log ("%s cannot be placed on the shore\n", name.c_str());
		tokens = 0;		// stop agent from running
	}

	Logger::Instance().Log ("creating %s\n", name.c_str());
}

// ===================================================================
// Execute one timestep
// ===================================================================
bool ShoreLineAgent::Execute ()
{
	Params& params = Params::Instance();

	if (tokens == 0)
	{
		return false;
	}

	tokens--;
	int height = Executive::Instance().getHeight(location);

	// once we hit high ground, start looking for another place to work
	if (height > params.beach_highland_limit)
	{
		if (!Executive::Instance().random_boundary(location))
		{
			Logger::Instance().Log ("%s cannot be placed on the shore\n", name.c_str());
			tokens = 0;		// stop agent from running
		}
		//else
		//{
		//	Logger::Instance().Log ("%s trying to avoid running through mountain\n", name.c_str());

		//}
	}

	TextureOp textureOp(TEXTURE_SAND);
	SetHeightOp altitudeOp (params.beach_min_alt, params.beach_max_alt);

	//altitudeOp.setOverride(true);
	Executive::Instance().operateArea(location, altitudeOp);

	//textureOp.setOverride(true);

	Executive::Instance().operateArea(location, textureOp);

	SmootherOp smootherOp;
	//smootherOp.setOverride(true);
	Executive::Instance().operateArea(location, smootherOp);

	Executive::Instance().fixArea(location);



//	Executive::Instance().smoothArea(location);
//	Executive::Instance().smoothArea(location);

	miniWalk(location);

	if (! adjacentShore (location))
	{
		if (!Executive::Instance().random_boundary(location))
		{
			Logger::Instance().Log ("%s cannot be placed on the shore\n", name.c_str());
			tokens = 0;		// stop agent from running
		}
		else
		{
			//Logger::Instance().Log ("%s jumping to new area %d,%d\n", name.c_str(), location.x, location.y);
		}
	}

//	Logger::Instance().Log ("%s moving to %d,%d\n", name.c_str(), location.x, location.y);
	return true;
}

bool ShoreLineAgent::adjacentShore(Point &p)
{
	Point neighbor;

	int dir = rand() % 8;

	for (int i = 0; i < 8; i++)
	{
		if (Executive::Instance().StepDir(location, neighbor, dir))
		{
			if ((! Executive::Instance().isFixed(neighbor)) && (Executive::Instance().on_shore(neighbor)))
			{
				p.x = neighbor.x;
				p.y = neighbor.y;
				return true;
			}
		}
		++dir %= 8;
	}

	return false;
}

void ShoreLineAgent::miniWalk (Point& location)
{
	Params& params = Params::Instance();
	Point current(location);
	int walk_size = rand() % params.beach_walk_variance + params.beach_walk_min;

	TextureOp textureOp(TEXTURE_SAND);
	SetHeightOp altitudeOp (params.beach_min_alt, params.beach_max_alt);

	textureOp.setOverride(true);
	altitudeOp.setOverride(true);

	for (int i = 0; i < walk_size; i++)
	{
		for (int j = 0; j < params.beach_interior_points; j++)
		{
			Point p(current);
			if (Executive::Instance().findInterior(p, params.beach_interior_distance))
			{
				Executive::Instance().operateArea(p, altitudeOp);
				Executive::Instance().operateArea(p, textureOp);

				Executive::Instance().fixArea(p);

				Executive::Instance().smoothArea(p);
				Executive::Instance().smoothArea(p);
			}
			else
			{
	//			Logger::Instance().Log ("%s: could not find interior point\n", name.c_str());
			}
		}

		int dir = rand() % 8;
		Executive::Instance().StepDir (current, current, dir, params.beach_interior_distance);
	}
}