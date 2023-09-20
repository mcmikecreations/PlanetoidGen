#include "TerrainOp.h"
#include "executive.h"
#include "logger.h"
#include "params.h"

TerrainOp::TerrainOp ()
{
	overrideFixed = false;
	delta = 0;
}
void TerrainOp::setOverride (bool b)						
{
	overrideFixed = b;
}

bool TerrainOp::mayOverride()
{
	return overrideFixed;
}

void TextureOp::Execute(Point &p)
{

	bool fixed = Executive::Instance().isFixed(p);

	if (Executive::Instance().inOcean(p))
	{
		return;
	}

	if (mayOverride())
	{
		if (fixed)
		{
			Executive::Instance().unfix(p);
		}
	}

	Executive::Instance().texturePoint(p, texture_id);

	if (mayOverride() && fixed)
	{
		Executive::Instance().fixPoint(p);
	}
}

void SetInsertOp::Execute(Point &p)
{
	points->insert (p);
}

void SetHeightOp::Execute(Point &p)
{
	int height;

	if (Executive::Instance().inOcean(p))
	{
		return;
	}

	if (range_max == 0)
	{
		height = range_min;
	}
	else
	{
		int range = abs(range_max - range_min);
		height = rand() % range + range_min;
		//Logger::Instance().Log ("SetHeightOp sets altitude via rand to %d\n", height);
	}

	int delta = getDelta();
	if (delta)
	{
		height -= delta;
	}

	bool fixed = Executive::Instance().isFixed(p);

	if (mayOverride())
	{
		if (fixed)
		{
			if (Executive::Instance().isWatched(p))
			{
				Logger::Instance().Log ("*** SetHeight overriding fix on watch point\n");
			}

			Executive::Instance().unfix(p);
		}
	}

	Executive::Instance().setHeight(p, height);

	if (mayOverride() && fixed)
	{
		Executive::Instance().fixPoint(p);
	}
}

void SmootherOp::Execute(Point& p)
{
	bool fixed = Executive::Instance().isFixed(p);

	if (mayOverride() && fixed)
	{
			if (Executive::Instance().isWatched(p))
			{
				Logger::Instance().Log ("*** Smoother overriding fix on watch point\n");
			}

		Executive::Instance().unfix(p);
	}

	Executive::Instance().smoothPoint(p);

	if (mayOverride() && fixed)
	{
		Executive::Instance().fixPoint(p);
	}
}

void FixPointOp::Execute(Point &p)
{
	Executive::Instance().fixPoint(p);
}

void RoughenerOp::Execute(Point& p)
{
	Params& params = Params::Instance();
	int delta = 0;

	bool fixed = Executive::Instance().isFixed(p);
	if (fixed && (!mayOverride()))
	{
		return;
	}

	int height = Executive::Instance().getHeight(p);
	if (height < params.beach_max_alt)
	{
		return;
	}

	if (mayOverride() && fixed)
	{
		Executive::Instance().unfix(p);
	}

	if (rand() % 100 < probability)
	{
		if (rand() % 2 == 1)
		{
			delta = -1 * (rand() % variance);
		}
		else
		{
			delta = (rand() % variance);
		}

		height += delta;
		height = std::max(height, 1);

		Executive::Instance().setHeight(p, height);
		// Logger::Instance().Log ("adding %d to point, resulting in %d\n", delta, height);
	}

	if (mayOverride() && fixed)
	{
		Executive::Instance().fixPoint(p);
	}
}