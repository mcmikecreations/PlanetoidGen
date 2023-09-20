#ifndef SMOOTH_AGENT_H
#define SMOOTH_AGENT_H

#include "agent.h"
#include "point.h"
#include "TerrainOp.h"

class SmoothAgent : public Agent
{
public:
		SmoothAgent (int _tokens);
		bool Execute ();

		void moveTo (Point& next);
		void setSmoother (bool allowOverride);
		void setSweepMode()		{randomWalk = false;}
private:

		Point location;
		Point initial_location;
		static int count;
		int reset_tokens;
		unsigned long weightedAverageHeight ();

		bool randomWalk;
		bool use_smoother;
		SmootherOp smoother;
};

#endif
