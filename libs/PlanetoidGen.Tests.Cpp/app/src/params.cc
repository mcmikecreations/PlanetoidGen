#include "params.h"

Params::Params ()
{
	seed = 0;
	x_size = 512;
	y_size = 512;
	noise_size = 4000;
	height_limit = 65535;

	size = 78184;
	coverage = 50;

	// JonRender scale factors
	scale_x = 9000;
	scale_y = 2000;
	scale_z = 9000;

	// agent counts and tokens (0 means use default values based on map size)
	num_mountain_agents = 16;
	num_beach_agents = 1;
	num_smooth_agents = 1;
	num_hill_agents = 1;
	num_river_agents = 0;

	mountain_tokens = 32;
	beach_tokens = 0;
	smooth_tokens = 0;
	hill_tokens = 0;

	// mountain agent params
	mountain_max_alt = 20000;
	mountain_variance = 5000;
	mountain_width = 40;
	mountain_slope_max = 350;
	mountain_slope_min = 100;
	mountain_rough_prob = 50;
	mountain_rough_var = 100;

	foothill_freq = 40;
	foothill_min_length = 50;
	foothill_max_length = 80;

	// hill agent params
	hill_max_alt = 9000;
	hill_variance = 500;

	// coastline agent params
	action_size_min = 50;
	action_size_max = 1000;

	// river agent params
	min_river_length = 40;
	river_backoff = 5;
	river_initialdrop = 1500;		// amount to lower source of river
	river_heightlimit = 5000;		// altitude to stop moving uphill
	river_widen_freq = 20;
	river_initial_width = 3;
	river_slope = 5;
	river_max_shore = 900;
	river_min_mountain = 15000;
	river_mountain_coast_dist = 40;

	// beach agent params
	beach_highland_limit = 3000;
	beach_max_alt = 640;
	beach_min_alt = 600;
	beach_interior_points = 4;
	beach_interior_distance = 4;
	beach_walk_min = 2;
	beach_walk_variance = 7;

	// smooth agent params
	smooth_num_resets = 1;

	format = FORMAT_PNG;
}

Params& Params::Instance()
{
	if (_instance.get() == NULL)
	{
		_instance.reset (new Params);
	}

	return *_instance;
}
