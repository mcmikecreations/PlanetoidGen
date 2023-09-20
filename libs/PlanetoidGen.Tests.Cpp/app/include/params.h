#ifndef PARAMS_H
#define PARAMS_H

#include <string>
#include <memory>		// for unique_ptr
#include "image.h"

class Params
{
public:
	static Params& Instance();

	int x_size;							// map size
	int y_size;

	int size;								// number of land points
	std::string name;
	long seed;							// RNG seed
	int coverage;
	int scale_x;
	int scale_y;
	int scale_z;
	ImageFormat format;
	int page_size;						// num pixels on edge of a page
	int noise_size;						// random noise about midpoint
	int height_limit;

	// coastline agent params
	int action_size_min;				// range of points which Coastline agents prefer to operate on
	int action_size_max;

	// mountain agent params
	int mountain_max_alt;
	int mountain_variance;
	int mountain_width;
	int mountain_slope_max;
	int mountain_slope_min;
	int mountain_rough_prob;
	int mountain_rough_var;
	int foothill_freq;		
	int foothill_min_length;
	int foothill_max_length;

	// hill agent params
	int hill_max_alt;
	int hill_variance;

	// river agent params
	int min_river_length;
	int river_backoff;
	int river_initialdrop;
	int river_heightlimit;
	int river_widen_freq;
	int river_initial_width;
	int river_slope;
	int river_max_shore;		// how close to sea level to accept a shore point
	int river_min_mountain;
	int river_mountain_coast_dist;

	// beach agent params
	int beach_highland_limit;
	int beach_min_alt;
	int beach_max_alt;
	int beach_interior_points;
	int beach_interior_distance;
	int beach_walk_min;
	int beach_walk_variance;

	// smooth agent params
	int smooth_num_resets;

	// agent counts
	int num_mountain_agents;
	int num_beach_agents;
	int num_smooth_agents;
	int num_hill_agents;
	int num_river_agents;

	int mountain_tokens;
	int beach_tokens;
	int smooth_tokens;
	int hill_tokens;

	// derived values
	int num_x_pages;
	int num_y_pages;
protected:
	Params();
private:
	static std::unique_ptr<Params> _instance;
};

#endif
