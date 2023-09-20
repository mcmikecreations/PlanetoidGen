#include "SmoothAgent.h"
#include "executive.h"
#include "logger.h"
#include <sstream>
#include "params.h"

using namespace std;

int SmoothAgent::count = 0;

SmoothAgent::SmoothAgent(int _tokens)
{
	Params& params = Params::Instance();

	type = SMOOTH_AGENT;
	tokens = _tokens;

	reset_tokens = tokens / (params.smooth_num_resets + 1);
	if (reset_tokens < 1)
	{
		reset_tokens = 1;
	}

	runnable = true;
	use_smoother = false;
	randomWalk = true;

	count++;
	id = count;

	ostringstream buf;
	buf << "Smooth Agent #" << id;
	name = buf.str();

	if (!Executive::Instance().random_land(location))
	{
		Logger::Instance().Log ("%s cannot be placed on land\n", name.c_str());
		location.x = 0;
		location.y = 0;
	}
	else
	{
		unsigned long height = Executive::Instance().getHeight(location);

	//	Logger::Instance().Log ("%s starting at (%d,%d), altitude %d\n", name.c_str(), location.x, location.y, height);
	}

	smoother.setOverride(false);

	initial_location.x = location.x;
	initial_location.y = location.y;
}

// ===================================================================
// Execute one timestep
// ===================================================================
bool SmoothAgent::Execute ()
{
	Params& params = Params::Instance();

	if ((tokens == reset_tokens) && randomWalk)
	{
		moveTo (initial_location);
	}

	if (tokens == 0)
	{
	//	Logger::Instance().Log ("%s stopping\n", name.c_str());
		return false;
	}

	tokens--;

	if (randomWalk)
	{
		if (Executive::Instance().isWatched(location))
		{
			Logger::Instance().Log ("%s operating on a watch point\n", name.c_str(), location.x, location.y);
		}

		if (use_smoother)
		{
			Executive::Instance().operatePoint(location, smoother);
		}
		else if (! Executive::Instance().isFixed(location))
		{
			Executive::Instance().smoothPoint(location);
		}

		Executive::Instance().random_neighbor (location, location);
	}
	else
	{
		Executive::Instance().operatePoint(location, smoother);
		location.x++;
		if (location.x == params.x_size)
		{
			location.x = 0;
		}
	}

	return true;
}

// ===================================================================
// calculate a weighted average of nearby points
// ===================================================================
unsigned long SmoothAgent::weightedAverageHeight ()
{
		unsigned long sum = 0;

		for (int i = -1; i < 2; i++)
		{
			for (int j = -1; j < 2; j++)
			{
				Point p (location.x + i, location.y + j);

				unsigned long height = Executive::Instance().getHeight(p);

				if ((i == 0) && (j == 0))
				{
					sum += 3 * height;
				}
				else
				{
					sum += height;
				}
			}
		}

		return sum / 11;
}

void SmoothAgent::moveTo (Point& next)
{
	location.x = next.x;
	location.y = next.y;
}

void SmoothAgent::setSmoother (bool allowOverride)
{
	use_smoother = true;
	smoother.setOverride(allowOverride);
}