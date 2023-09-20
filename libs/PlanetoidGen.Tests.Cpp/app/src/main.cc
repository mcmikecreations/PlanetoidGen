#define TRACK_MEMORY 0

#if TRACK_MEMORY == 1
#include <cstdlib>
#include <new>
#include <limits>
#include <unordered_map>
#include <memory>

template <class T>
struct Mallocator
{
    typedef T value_type;

    Mallocator() = default;
    template <class U> constexpr Mallocator(const Mallocator <U>&) noexcept {}

    [[nodiscard]] T* allocate(std::size_t n) {
        if (n > std::numeric_limits<std::size_t>::max() / sizeof(T))
            throw std::bad_array_new_length();

        if (auto p = static_cast<T*>(std::malloc(n * sizeof(T)))) {
            return p;
        }

        throw std::bad_alloc();
    }

    void deallocate(T* p, std::size_t n) noexcept {
        std::free(p);
    }
};

template <class T, class U>
bool operator==(const Mallocator <T>&, const Mallocator <U>&) { return true; }
template <class T, class U>
bool operator!=(const Mallocator <T>&, const Mallocator <U>&) { return false; }

static size_t usedBytes = 0;
static size_t usedBytesTotal = 0;
static size_t usedBytesMax = 0;
using mymap = std::unordered_map<void*, size_t, std::hash<void*>, std::equal_to<void*>, Mallocator<std::pair<void* const, size_t>>>;
static mymap* usedAllocs = nullptr;

// Overloading Global new operator
void* operator new(size_t sz)
{
    if (usedAllocs == nullptr)
    {
        usedAllocs = new (malloc(sizeof(mymap))) mymap();
    }

    void* m = malloc(sz);

    usedBytes += sz;
    usedBytesTotal += sz;
    if (usedBytes > usedBytesMax) usedBytesMax = usedBytes;
    usedAllocs->insert({ m, sz });

    return m;
}
// Overloading Global delete operator
void operator delete(void* m)
{
    free(m);

    usedBytes -= (*usedAllocs)[m];
    usedAllocs->erase(m);
}
// Overloading Global new[] operator
void* operator new[](size_t sz)
{
    if (usedAllocs == nullptr)
    {
        usedAllocs = new (malloc(sizeof(mymap))) mymap();
    }

    void* m = malloc(sz);

    usedBytes += sz;
    usedBytesTotal += sz;
    if (usedBytes > usedBytesMax) usedBytesMax = usedBytes;
    usedAllocs->insert({ m, sz });

    return m;
}
// Overloading Global delete[] operator
void operator delete[](void* m)
{
    free(m);

    usedBytes -= (*usedAllocs)[m];
    usedAllocs->erase(m);
}

#endif

#include <stdio.h>
#include <time.h>
#include <stdlib.h>
#include <string.h>
#include <sstream>
#include <iostream>
#include <chrono>

#include "arglist.h"
#include "map.h"
#include "heightmap.h"
#include "cultural.h"
#include "logger.h"
#include "executive.h"
#include "math.h"

// Agents
#include "MountainAgent.h"
#include "SmoothAgent.h"
#include "ShoreLineAgent.h"
#include "RiverAgent.h"
#include "ErosionAgent.h"
#include "WaterModel.h"
#include "HillAgent.h"

#define LOGGING 1
#include "params.h"

using namespace std;

FILE *logfile = NULL;
int repeatTimes = 0;

string exe_name;

const char *boolstring (bool flag);

void generate ();
void logParams ();

unique_ptr<Params> Params::_instance;

void usage ()
{
	fprintf (stderr, "Usage:  %s  [-seed seed]\n", exe_name.c_str());
	fprintf (stderr, "            [-x width] [-y height]\n");
	fprintf (stderr, "            [-name map-name]\n");
	fprintf (stderr, "            [-size n]\n");

	exit (1);
}

// ===========================================================
// process_arglist -- process an argument list
// ===========================================================
void process_arglist (Arglist *args)
{
	Params& p = Params::Instance();

	p.page_size = 0;

	for (unsigned int i = 0; i < args -> count(); i++)
	{
        if (args->getArg(i).compare("-rep") == 0)
        {
            repeatTimes = atol(args->getArg(++i).c_str());
            continue;
        }

        if (args->getArg(i).compare("-seed") == 0)
		{
			p.seed = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-ned") == 0)
		{
			p.format = FORMAT_TGA;
			//p.make_flat = true;
			continue;
		}

		if (args->getArg(i).compare("-name") == 0)
		{
			p.name = args->getArg(++i);
			continue;
		}

		if (args->getArg(i).compare("-x") == 0)
		{
			p.x_size = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-y") == 0)
		{
			p.y_size = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-size") == 0)
		{
			p.size = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-coverage") == 0)
		{
			p.coverage = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-scale_x") == 0)
		{
			p.scale_x = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-scale_y") == 0)
		{
			p.scale_y = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-scale_z") == 0)
		{
			p.scale_z = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-pagesize") == 0)
		{
			p.page_size = atol (args->getArg(++i).c_str());
			continue;
		}

		// ===== Agent counts and tokens  =====
		if (args->getArg(i).compare("-num_mountain_agents") == 0)
		{
			p.num_mountain_agents = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-num_beach_agents") == 0)
		{
			p.num_beach_agents = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-num_smooth_agents") == 0)
		{
			p.num_smooth_agents = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-num_hill_agents") == 0)
		{
			p.num_hill_agents = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-num_river_agents") == 0)
		{
			p.num_river_agents = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-mountain_tokens") == 0)
		{
			p.mountain_tokens = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-beach_tokens") == 0)
		{
			p.beach_tokens = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-smooth_tokens") == 0)
		{
			p.smooth_tokens = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-hill_tokens") == 0)
		{
			p.hill_tokens = atol (args->getArg(++i).c_str());
			continue;
		}

		// =====  Coastline Agent Params =====
		if (args->getArg(i).compare("-action_size_min") == 0)
		{
			p.action_size_min = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-action_size_max") == 0)
		{
			p.action_size_max = atol (args->getArg(++i).c_str());
			continue;
		}

		// =====  Mountain Agent Params =====

		if (args->getArg(i).compare("-mountain_max_alt") == 0)
		{
			p.mountain_max_alt = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-mountain_variance") == 0)
		{
			p.mountain_variance = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-mountain_width") == 0)
		{
			p.mountain_width = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-mountain_slope_min") == 0)
		{
			p.mountain_slope_min = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-mountain_slope_max") == 0)
		{
			p.mountain_slope_max = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-mountain_rough_prob") == 0)
		{
			p.mountain_rough_prob = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-mountain_rough_var") == 0)
		{
			p.mountain_rough_var = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-foothill_freq") == 0)
		{
			p.foothill_freq = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-foothill_min_length") == 0)
		{
			p.foothill_min_length = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-foothill_max_length") == 0)
		{
			p.foothill_max_length = atol (args->getArg(++i).c_str());
			continue;
		}

		// =====  Mountain Agent Params =====

		if (args->getArg(i).compare("-hill_max_alt") == 0)
		{
			p.hill_max_alt = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-hill_variance") == 0)
		{
			p.hill_variance = atol (args->getArg(++i).c_str());
			continue;
		}

		// ===== River Agent Params =====

		if (args->getArg(i).compare("-min_river_length") == 0)
		{
			p.min_river_length = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_backoff") == 0)
		{
			p.river_backoff = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_initialdrop") == 0)
		{
			p.river_initialdrop = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_heightlimit") == 0)
		{
			p.river_heightlimit = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_widen_freq") == 0)
		{
			p.river_widen_freq = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_initial_width") == 0)
		{
			p.river_initial_width = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_slope") == 0)
		{
			p.river_slope = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_max_shore") == 0)
		{
			p.river_max_shore = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-river_min_mountain") == 0)
		{
			p.river_min_mountain = atol (args->getArg(++i).c_str());
			continue;
		}

		if (args->getArg(i).compare("-river_mountain_coast_dist") == 0)
		{
			p.river_mountain_coast_dist = atol (args->getArg(++i).c_str());
			continue;
		}

		// ===== Beach Agent Params =====

		if (args->getArg(i).compare("-beach_highland_limit") == 0)
		{
			p.beach_highland_limit = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-beach_min_alt") == 0)
		{
			p.beach_min_alt = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-beach_max_alt") == 0)
		{
			p.beach_max_alt = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-beach_interior_points") == 0)
		{
			p.beach_interior_points = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-beach_interior_distance") == 0)
		{
			p.beach_interior_distance = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-beach_walk_min") == 0)
		{
			p.beach_walk_min = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-beach_walk_variance") == 0)
		{
			p.beach_walk_variance = atol (args->getArg(++i).c_str());
			continue;
		}
		if (args->getArg(i).compare("-smooth_num_resets") == 0)
		{
			p.smooth_num_resets = atol (args->getArg(++i).c_str());
			continue;
		}
	}

	if (p.name.size() == 0)
	{
		std::ostringstream terrainName;

		terrainName << "seed" << p.seed;
		p.name = terrainName.str();
	}

	if (p.page_size == 0)
	{
		p.page_size = p.x_size;
	}

	p.num_x_pages = (p.x_size + p.page_size - 1) / p.page_size;
	p.num_y_pages = (p.y_size + p.page_size - 1) / p.page_size;

#if LOGGING
	logParams ();
#endif
}

const char *boolstring (bool flag)
{
	if (flag)
		return "true";
	else
		return "false";
}

void logParams ()
{
	Params& params = Params::Instance();

	Logger::Instance().Log ("seed = %d\n", params.seed);
	Logger::Instance().Log ("x_size = %d, y_size = %d\n", params.x_size, params.y_size);
	Logger::Instance().Log ("noise_size = %d\n", params.noise_size);
	Logger::Instance().Log ("altitude limit = %d\n", params.height_limit);
	Logger::Instance().Log ("coverage = %d\n", params.coverage);
	Logger::Instance().Log ("num_mountain_agents = %d\n", params.num_mountain_agents);
	Logger::Instance().Log ("num_beach_agents = %d\n", params.num_beach_agents);
	Logger::Instance().Log ("num_smooth_agents = %d\n", params.num_smooth_agents);
	Logger::Instance().Log ("num_hill_agents = %d\n", params.num_hill_agents);
	Logger::Instance().Log ("num_river_agents = %d\n", params.num_river_agents);
	Logger::Instance().Log ("mountain tokens = %d\n", params.mountain_tokens);
	Logger::Instance().Log ("beach tokens = %d\n", params.beach_tokens);
	Logger::Instance().Log ("smooth tokens = %d\n", params.smooth_tokens);
	Logger::Instance().Log ("hill tokens = %d\n", params.hill_tokens);
	Logger::Instance().Log ("mountain max alt = %d\n", params.mountain_max_alt);
	Logger::Instance().Log ("mountain variance = %d\n", params.mountain_variance);
	Logger::Instance().Log ("mountain width = %d\n", params.mountain_width);
	Logger::Instance().Log ("mountain slope min = %d, slope max = %d\n", params.mountain_slope_min, params.mountain_slope_max);
	Logger::Instance().Log ("mountain rough prob = %d/100\n", params.mountain_rough_prob);
	Logger::Instance().Log ("mountain rough var = %d\n", params.mountain_rough_var);
	Logger::Instance().Log ("foothill freq = %d\n", params.foothill_freq);
	Logger::Instance().Log ("foothill min length = %d, max length = %d\n", params.foothill_min_length, params.foothill_max_length);
	Logger::Instance().Log ("hill max alt = %d\n", params.hill_max_alt);
	Logger::Instance().Log ("hill variance = %d\n", params.hill_variance);
	Logger::Instance().Log ("action size min = %d, max = %d\n", params.action_size_min, params.action_size_max);
	Logger::Instance().Log ("minimum river length = %d, initial dropoff = %d, height limit = %d\n",
		params.min_river_length, params.river_initialdrop, params.river_heightlimit);
	Logger::Instance().Log ("river widen freq = %d, initial width = %d, slope = %d\n",
		params.river_widen_freq, params.river_initial_width, params.river_slope);
	Logger::Instance().Log ("river max shore = %d, min mountain = %d, min coast distance to mountain = %d\n",
		params.river_max_shore, params.river_min_mountain, params.river_mountain_coast_dist);
	Logger::Instance().Log ("beach highland limit = %d, min alt = %d, max alt = %d\n",
		params.beach_highland_limit, params.beach_min_alt, params.beach_max_alt);
	Logger::Instance().Log ("beach interior points = %d, interior distance = %d\n",
		params.beach_interior_points, params.beach_interior_distance);
	Logger::Instance().Log ("beach walk min = %d, walk variance = %d\n",
		params.beach_walk_min, params.beach_walk_variance);
	Logger::Instance().Log ("smooth num resets = %d\n", params.smooth_num_resets);
}

int main (int argc, char **argv)
{
	Arglist args;
	Params& params = Params::Instance();
	char loaded_argfile = 0;
	char seed_set = 0;
	char use_raw = 0;

	// save a copy of the exe name off for error messages
	exe_name = string(argv[0]);

	//if (argc < 2)
	//{
	//	usage ();
	//}

#if LOGGING
	if ((logfile = fopen ("log.txt", "w")) == NULL)
	{
		perror ("log.txt");
		exit (1);
	}
	Logger::Instance().SetLog (logfile);
#endif

	//if (strcmp (argv[1], "-l") == 0)
	//{
	//	args.Load (argv[2]);
	//	loaded_argfile = 1;
	//	process_arglist (&args);
	//}
	//else
	//{
		args.Set (argc, argv);
		process_arglist (&args);
	//}

	// Save a new argfile (for later reloading) unless this run uses
	// a loaded argfile.  This means that complex command-line settings
	// are automatically saved in the current directory.

	if (! loaded_argfile)
	{
		ostringstream argfile;
		argfile << params.name << ".args";

		args.Save (argfile.str().c_str());
	}

    using Clock = std::chrono::high_resolution_clock;

    for (int i = 0; i < repeatTimes; ++i)
    {
#if TRACK_MEMORY == 1
        usedBytes = 0;
        usedBytesMax = 0;
        usedBytesTotal = 0;
#endif
        srand(params.seed);

#if _WIN32
        system("del /q .\\*.png");
#else
        system("rm -f ./*.png");
#endif

        auto begin = Clock::now();
        generate();
        auto end = Clock::now();

#if TRACK_MEMORY == 1
        std::cout << "Time: " << std::chrono::duration_cast<std::chrono::microseconds>(end - begin).count() << " Bytes total: " << usedBytesTotal << " Bytes max: " << usedBytesMax << std::endl;
#else
        std::cout << "Time: " << std::chrono::duration_cast<std::chrono::microseconds>(end - begin).count() << std::endl;
#endif
    }

#if LOGGING
	fclose (logfile);
#endif
    return 0;
}

void generate ()
{
	Params& params = Params::Instance();

	Logger::Instance().Log ("starting map generation at %s\n", Executive::Instance().currentTime().c_str());

	Map *map = new Map (params.x_size, params.y_size);
	map -> SetMode (rgba_8);
	map -> Set_Coverage (params.coverage);

	Logger::Instance().Log ("generating mask\n");
	map -> generate_mask ();
	map->Write("mask.png");

	Logger::Instance().Log ("running heightmap agents\n");

	Executive::Instance().setMask(map);

	Agent *agent;
	Executive::Instance().Setup();

	// attempt to scale the agent tokens based on map size
	int totalVertices = params.x_size * params.y_size;

	int smoothTokens = (int) (sqrt((float) totalVertices) * 3.0);

	for (int i = 0; i < params.num_mountain_agents; i++)
	{
		MountainAgent *agent = new MountainAgent(params.mountain_tokens);
		agent->setAltitudePreferences(params.mountain_max_alt, params.mountain_variance);
		Executive::Instance().addAgent(agent);
	}

	for (int i = 0; i < params.num_hill_agents; i++)
	{
		MountainAgent *agent = new MountainAgent(params.hill_tokens);
		agent->setAltitudePreferences(params.hill_max_alt, params.hill_variance);
		Executive::Instance().addAgent(agent);
	}

	// the sweeping smoothers
	for (int i = 0; i < params.y_size; i++)
	{
		SmoothAgent *agent = new SmoothAgent(2 * params.x_size);
		agent->setSweepMode();
		Point p(0, i);
		agent->moveTo(p);
		Executive::Instance().addAgent(agent);
	}

	for (int i = 0; i < params.num_smooth_agents; i++)
	{
		agent = new SmoothAgent(params.smooth_tokens);
		Executive::Instance().addAgent(agent);
	}

	for (int i = 0; i < params.num_river_agents; i++)
	{
		agent = new RiverAgent(700);
		Executive::Instance().addAgent(agent);
	}

	for (int i = 0; i < params.num_beach_agents; i++)
	{
		agent = new ShoreLineAgent (params.beach_tokens);
		Executive::Instance().addAgent(agent);
	}

//	agent = new ErosionAgent();
//	Executive::Instance().addAgent(agent);


	Executive::Instance().Run();


	//for (int i = 0; i < 10; i++)
	//{
	//	int len = rand() % 500 + 400;
	//	agent = new HillAgent(len);
	//	Executive::Instance().addAgent(agent);
	//}
	//Logger::Instance().Log ("restarting executive for another phase\n");
	//Executive::Instance().Run();
	Executive::Instance().PostRun();

//	WaterModel::Instance().setFlowVectors();
//	WaterModel::Instance().printAllFlows ();

//	Logger::Instance().Log ("generating culture\n");
//	Culture_Generator *culture = new Culture_Generator ();
//	culture -> generate ();

	Executive::Instance().writeHeightmap();

	Logger::Instance().Log ("finishing map generation at %s\n", Executive::Instance().currentTime().c_str());
//	culture -> SplitMap ();
//	delete culture;

	delete map;
}

