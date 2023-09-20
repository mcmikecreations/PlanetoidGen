#include "HillAgent.h"

#include "executive.h"
#include "MountainAgent.h"
#include "logger.h"
#include <sstream>

using namespace std;

int HillAgent::count = 0;

HillAgent::HillAgent (int _tokens)
{
	type = HILL_AGENT;

	count++;
	id = count;

	ostringstream buf;
	buf << "Hill Agent #" << id;
	name = buf.str();

	tokens = _tokens;
	runnable = true;
	initialized = false;
}

// ===================================================================
// Execute one timestep
//
// Pick a point, elevate and smooth
// ===================================================================
bool HillAgent::Execute ()
{
	if (tokens == 0)
	{
		Logger::Instance().Log ("%s finishing\n", name.c_str());
		return false;
	}

	if (! initialized)
	{
		initialized = true;
		MountainAgent *mountain = Executive::Instance().randomMountainAgent();
		if (! mountain)
		{
			Logger::Instance().Log ("%s: could not find a mountain agent\n", name.c_str());
			return false;
		}
	
		Point center;

		if (! mountain->randomBase(location, center))
		{
			Logger::Instance().Log ("%s: could not find a base point for %s\n", name.c_str(),
				mountain->getName().c_str());
			return false;
		}

		int dx = location.x - center.x;
		int dy = location.y - center.y;

		location.x += (dx * 2);
		location.y += (dy * 2);

		//int dir = rand() % 8;
		//int dist = rand() % 20 + 20;
		//Executive::Instance().StepDir (location, location, dir, dist);
	}

	tokens--;

	int height = Executive::Instance().getHeight(location);
	height = max(height, 25000);
	Executive::Instance().setHeight(location, height);
//	Executive::Instance().smoothPoint(location);

	int dir = rand() % 8;
	Point p;
	for (int i = 0; i < 8; i++)
	{
		Executive::Instance().StepDir (location, p, (dir + i) % 8);

		if (Executive::Instance().on_land(p))
		{
			location = p;
			return true;
		}
	}


	Logger::Instance().Log ("%s cannot find an adjacent point on map\n", name.c_str());
	return false;
}