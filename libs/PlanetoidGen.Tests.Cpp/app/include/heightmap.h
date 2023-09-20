#ifndef HEIGHTMAP_H
#define HEIGHTMAP_H

#include "image.h"
#include "point.h"
#include "pointset.h"
#include "index.h"
#include "map.h"

/**
 * \brief A Heightmap is a rectangular set of elevation points
 *        associated with each point is texture information.
 *
 * The principle job of the Heightmap class is to generate height data
 * from a source mask.
 * Heightmaps are stored as 16-bit greyscale images.
*/

//typedef enum {DIR_RIGHT, DIR_LEFT, DIR_DOWN, DIR_UP, DIR_UR, DIR_UL, DIR_LL, DIR_LR} Direction;

typedef enum {DIR_UP, DIR_UR, DIR_RIGHT, DIR_LR, DIR_DOWN, DIR_LL, DIR_LEFT, DIR_UL} Direction;

class Heightmap : public Image
{
private:
	// Map *mask;

	// -----------------------------------------------------------------
	// mask generation functions

	void BlockWalk (PointSet& points, int level, int walksize);
	void SmoothBlock (Point &p, int size);
	bool BlockStep (Point &current, Point& next, int size);
	unsigned long BlockValue (Point &p, int size);
	void SetBlock (Point &p, int size, unsigned long value);
	bool on_land (int x, int y);

	// -----------------------------------------------------------------
	// mask smoothing functions
 
	/**
     * Generate random heights within a band around 0.
     */

	void shock_map (int num_points);
	void blotch (int x, int y, int steps, unsigned long magnitude);
	void smooth_map ();
	void smooth_cell (int x, int y);
	void smooth_walk (int x, int y, int steps);
	void smooth_plus (int x, int y);
	unsigned long CellValue (int x, int y);

	/**
	 * Write one page of height data to disk
	 */
	void write_height_page (int page_x, int page_z);

	/**
	 * Generate a configuration file for the PLSM scene manager (Ogre)
	 */
	//void generate_plsm_cfg ();

public:
	Heightmap (int x, int y);
	~Heightmap ();

	unsigned long Max ();
	unsigned long Min ();
	bool random_neighbor (Point& src, Point& neighbor);
	bool random_point_on_mask (Point& point);
	bool neighbor (Point& seed, Point& neighbor, int direction);
	void randomize (unsigned long band_size);
	bool StepDir (Point& src, Point& dst, int direction, int delta = 1);

	/**
	 * Generate height data
	 */
	// void generate ();

	/**
	 * Split the height data into a set of page-sized files
	 */
//	void SplitMap ();

	void Scale (unsigned long max);
};

#endif
