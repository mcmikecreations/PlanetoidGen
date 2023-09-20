#include "cultural.h"
#include "logger.h"
#include "culture_data.h"

#include <fstream>
#include <sstream>
#include <stdio.h>
#include <errno.h>
#include <math.h>
#include <cstring>
#include "params.h"
#include "executive.h"

Culture_Generator::Culture_Generator ()
{
	texture = NULL;
}

Culture_Generator::~Culture_Generator ()
{
	if (texture)
	{
		delete texture;
	}
}


// -------------------------------------------------------------
// generate
/// @brief 	Create a list of flora/cultural objects
// -------------------------------------------------------------
void Culture_Generator::generate (void)
{
	Params& params = Params::Instance();
	int lava = 11;

	generate_textures ();
	if (! texture)
	{
		Logger::Instance().Log ("textures not generated prior to cultural\n");
		return;
	}

	// make a randomly-shaped grassy area
	Point p;

	std::ofstream culture;
	culture.open ("./split/cultural.xml", std::ios::out);

	if (! culture)
	{
		Logger::Instance().Log ("cannot open cultural file for output\n");
		Logger::Instance().Log ("./split/cultural.xml:  %s\n",
			strerror (errno));
	}

	culture << "<?xml-version=\"1.0\" ?>" << std::endl;
	culture << "<Cultural>" << std::endl;
	culture << "     <--! Static objects other than terrain -->" << std::endl;
	culture << std::endl;

	if (find_flat_region (p))
	{
		Logger::Instance().Log ("flat region around (%d,%d)\n", p.x, p.y);

		int grass3 = 7;
		PointSet grassy_region (params.x_size, params.y_size);

		texture_blob (grassy_region, p.x, p.y, grass3, 200);
		Logger::Instance().Log ("%d points in grassy region\n",
			grassy_region.size());

		// build forest region
		for (int i = 0; i < 10; i++)
		{
			Point seed;

			if (!grassy_region.random_member (seed))
			{
				Logger::Instance().Log ("unable to obtain random member of grassy blob, aborting texturing\n");
				return;
			}

			float rotation = (rand() % 360000) / 1000.0f;
			float scale = (0.9f + (rand() % 200) / 100.0f);

			switch (rand() % 2)
			{
				case 0:
					place_mesh (culture, std::string("weed2.mesh"), (float) seed.x, (float) seed.y, rotation, scale);
					break;
				case 1:
					place_mesh (culture, std::string("bush9.mesh"), (float) seed.x, (float) seed.y, rotation, scale);
					break;
			}

			int lava = 11;
			texture -> SetPrimary (seed.x, seed.y, indexOf (lava), 255);
		}
	}
	else
	{
		Logger::Instance().Log ("no flat region found on terrain\n");
	}

	culture << "</Cultural>" << std::endl << std::endl;
	// add an ocean plane

	culture << "<Water>" << std::endl;
	culture << "     <!-- Water Planes -->" << std::endl;
	culture << std::endl;
	culture << "     <!-- Main Ocean -->" << std::endl;

	cPoint tx_p1, tx_p2;

	Logger::Instance().Log ("translating corners of ocean\n");
	translate (0, 0, tx_p1);
	translate (params.x_size, params.y_size, tx_p2);

	culture << "     <Quad ";
	culture << "X1=\"" << tx_p1. x << "\" ";
	culture << "Y1=\"" << tx_p1.y << "\" ";
	culture << "Z1=\"" << tx_p1.z + 40 << "\" ";

	culture << "X2=\"" << tx_p2.x << "\" ";
	culture << "Y2=\"" << tx_p2.y << "\" ";
	culture << "Z2=\"" << tx_p2.z + 40 << "\" ";

	culture << "> </Quad>" << std::endl;
	culture << "</Water>" << std::endl;

	culture.close();



	float maxAlt = (float) Executive::Instance().maxAltitude();
	Logger::Instance().Log ("maximum altitude (unscaled) is %f\n", maxAlt);
}

// ==========================================================
// translate
/// @brief Translate a point from generator coordinates to display coordinates
///
/// The displayed map is shifted and scaled, so this translation allows
/// points to be converted.  This is useful, for example, to place objects.
///
/// The center of a page is at 0,0; the edges of the page extend from negative (scale/2) to positive (scale/2).
///
/// @param x,y	The source point to translate
/// @return		The point translated to display coordinates
// ==========================================================

void Culture_Generator::translate (int x, int y, cPoint& translated)
{
	Params& params = Params::Instance();
	Point p(x,y);

	Logger::Instance().Log ("translate (%d,%d) into 3D space\n", x, y);
	Logger::Instance().Log ("%d x pages, %d y pages\n", params.num_x_pages, params.num_y_pages);
	Logger::Instance().Log ("page size is %d\n", params.page_size);

	float altitude = (float) Executive::Instance().getHeight(p);

#if 0
	int page_x = x / params.page_size;
	int page_y = y / params.page_size;

	int offset_x = x % params.page_size;
	int offset_y = y % params.page_size;
#endif

	// convert x,y coords to fractions of a page
	float total_x = (float) params.num_x_pages * (float) params.page_size;
	float total_y = (float) params.num_y_pages * (float) params.page_size;

	Logger::Instance().Log ("total of %f x-vertexes, and %f y-vertexes\n", total_x, total_y);

	float fractional_x = ((float) x / total_x);
	float fractional_y = ((float) y / total_y);

	Logger::Instance().Log ("fractional x = %f, fractional y = %f\n", fractional_x, fractional_y);

	// entire map is translated so that the center is 0,0
	// we divide the total length and width by 2, and subtract this from each coordinate
	float x_shift = (params.num_x_pages * params.page_size) / 2.0f;
	float y_shift = (params.num_y_pages * params.page_size) / 2.0f;

	float shifted_x = x - x_shift;
	float shifted_y = y - y_shift;

	Logger::Instance().Log ("x_shift = %f, y_shift = %f, shifted-x = %f, shifted-y = %f\n",
		x_shift, y_shift, shifted_x, shifted_y);

	translated.x = (shifted_x / params.page_size) * params.scale_x;
	translated.y = (shifted_y / params.page_size) * params.scale_z;

#if 0
	// finally, we scale this point to match the terrain scaling
	// (note that 2D XY correspond to 3D XZ)
	// (but cPoint uses Z as up rather than Y; so Y is the altitude scale)
	translated.x = shifted_x * params.scale_x;
	translated.y = shifted_y * params.scale_z;
#endif

	Logger::Instance().Log ("scaling x by %d, scaling z by %d\n", params.scale_x, params.scale_z);
	Logger::Instance().Log ("translated x,y = (%f, %f)\n", translated.x, translated.y);

#if 0
	float half_width = (params.num_x_pages / 2);
	float half_height = (params.num_y_pages / 2);
	x /= params.page_size;
	y /= params.page_size;
	
	Logger::Instance().Log ("alt = %d, page_x = %d, page_y = %d\n", altitude, x, y);

	float tx = (float) ((x * params.scale_x) - (half_width * params.scale_x));
	float ty = (float) ((y * params.scale_z) - (half_height* params.scale_z));

	Logger::Instance().Log ("tx = %f, ty = %f\n", tx, ty);

	translated.x = tx;
	translated.y = ty;
#endif

	float maxAlt = (float) Executive::Instance().maxAltitude();
	float altScale = params.scale_y / maxAlt;

	Logger::Instance().Log ("altitude scaling factor: %f, maxAlt = %f\n", altScale, maxAlt);

	translated.z = altitude * altScale;

}

// ==========================================================
//  place_mesh
/// @brief Write records to the cultural file which place a mesh at a location
///
/// Placing a mesh requires writing a cultural type record (identifying the
/// object as a mesh), and then writing a mesh record which contains the
/// translated location and orientation of the mesh.  These are steps which
/// can easily be overlooked, so this method was created to streamline the
/// process.
// ==========================================================
void Culture_Generator::place_mesh (std::ofstream &culture, std::string name, float x, float y, 
	float yaw, float scale)
{
	// translate the destination coords from heightmap to the scaled run-time
	// coords which the renderer will use
	cPoint tx_p;
	translate ((int) x, (int) y, tx_p);

	culture << "     <Mesh Name=\"" << name << "\" ";
	culture << "X=\"" << tx_p.x << "\" ";
	culture << "Y=\"" << tx_p.y << "\" ";
	culture << "Z=\"" << tx_p.z << "\" ";
	culture << "Yaw=\"" << yaw << "\" ";
	culture << "Scale=\"" << scale << "\"";
	culture << "> </Mesh>" << std::endl;
}

// ==========================================================
//  generate_textures
/// @brief	Generate the texture index for this map
//
/// @note This is a basic texturing, no peephole optimization is done,
/// so shorelines may have water crawling up adjacent land.
///
/// @note Also note that texture indexes are hard-coded for now
// ==========================================================
void Culture_Generator::generate_textures (void)
{
	Params& params = Params::Instance();

	texture = new Index (params.x_size, params.y_size);
	texture -> SetOrigin (origin_bottom);
	texture -> SetMode (rgba_8);

	for (int x = 0; x < params.x_size; x++)
	{
		for (int y = 0; y < params.y_size; y++)
		{
			Point p(x,y);

			unsigned long elev = Executive::Instance().getHeight(p);
			int grad = max_gradient (x, y);

			if (elev < 50)
			{
				int tex = 0 ;	// dirt

				texture -> SetPrimary (x, y, indexOf (tex), 255);
				texture -> SetSecondary (x, y, indexOf (tex), 255);
			}
			else
			{

				int tex = 0;	// land
				int rock = 8;

				if (grad > 2000)
					tex = 8;	// rock
				else
					tex = 3;	// grass

				texture -> SetPrimary (x, y, indexOf (tex), 255);
				texture -> SetSecondary (x, y, indexOf (tex), 255);
			}
		}
	}
	texture -> SetPrimary (250, 250, indexOf(14), 255);
	texture -> SetSecondary(250, 250, indexOf(14), 255);
	texture -> SetPrimary (251, 250, indexOf(14), 255);
	texture -> SetSecondary(251, 250, indexOf(14), 255);
	texture -> SetPrimary (251, 251, indexOf(14), 255);
	texture -> SetSecondary(251, 251, indexOf(14), 255);
	texture -> SetPrimary (250, 251, indexOf(14), 255);
	texture -> SetSecondary(250, 251, indexOf(14), 255);

	texture -> SetPrimary (275, 250, indexOf(14), 255);
	texture -> SetSecondary(275, 250, indexOf(14), 255);
	texture -> SetPrimary (276, 250, indexOf(14), 255);
	texture -> SetSecondary(276, 250, indexOf(14), 255);
	texture -> SetPrimary (276, 251, indexOf(14), 255);
	texture -> SetSecondary(276, 251, indexOf(14), 255);
	texture -> SetPrimary (275, 251, indexOf(14), 255);
	texture -> SetSecondary(275, 251, indexOf(14), 255);

	cPoint p;
	Logger::Instance().Log ("first spike\n");
	translate (250, 250, p);
	Logger::Instance().Log ("second spike\n");
	translate (275,250, p);
}

int
Culture_Generator::max_gradient (int x, int y)
{
	Point p(x,y);
	unsigned long elev = Executive::Instance().getHeight(p);
	int max = 0;

	for (int i = -1; i < 2; i++)
	{
		for (int j = -1; j < 2; j++)
		{
			Point p2(x+i,y+j);

			// skip center point
			if ((i == 0) && (j == 0))
				continue;
			unsigned long elev2 = Executive::Instance().getHeight(p2);
	
			int grad = elev - elev2;
			grad = abs(grad);

			if (grad > max)
			{
				max = grad;
			}
		}
	}

	// Logger::Instance().Log ("max grad = %d\n", max);
	return max;
}

// -------------------------------------------------------------
//  indexOf
/// @brief 	Convert a slice number to a texture index
/// @param slice	The index of the image within a texture atlas
/// @return 		An index usable by the IndexSplat shader
// -------------------------------------------------------------
int Culture_Generator::indexOf (int slice)
{
	// hardcode until such time as texture atlas is dynamically built
	int num_slices = 16;
	int slice_range = 256;

	return (slice * (slice_range/num_slices));
}

// ==========================================================
// TexturedAs -- determine if a point has the specified texture
// ==========================================================
bool Culture_Generator::TexturedAs (int x, int y, int texture_id)
{
	uchar tex = texture -> PrimaryTexture (x, y);
	if (tex == indexOf(texture_id))
	{
		return true;
	}
	else
	{
		return false;
	}
}

// ==========================================================
// AnySurrounding -- determine if any surrounding points have texture
// ==========================================================
bool Culture_Generator::AnySurrounding (int x, int y, int texture_id)
{
	for (int i = x - 1; i <= x + 1; i++)
	{
		for (int j = y - 1; j <= y + 1; j++)
		{
			// skip center point
			if ((i == x) && (j == y))
			{
				continue;
			}
			Point p(x,y);
			unsigned long elev = Executive::Instance().getHeight(p);

			if (TexturedAs (i, j, texture_id) && (elev > 0))
			{
				return true;
			}
		}
	}

	return false;
}

// -------------------------------------------------------------
// texture_blob -- Texture a randomly shaped area containing the passed pt
//
// Inputs:
//	  x,y:  Starting point, this is not necessarily centered
//    tex:  The texture to apply
//    size:  The number of points to attempt to texture
// -------------------------------------------------------------
bool Culture_Generator::texture_blob (PointSet& points, int x, int y, int tex, int size)
{
	Params& params = Params::Instance();

	int placed = 0;

	PointSet boundary(params.x_size, params.y_size);

	texture -> SetPrimary (x, y, indexOf (tex), 255);
	texture -> SetSecondary (x, y, indexOf (tex), 255);
	points.insert (x, y);
	boundary.insert (x, y);

	while (placed < size)
	{
		Point seed;
		Point neighbor;

		if (! boundary.random_member (seed))
		{
			Logger::Instance().Log ("returning early from texture_blob\n");
			return true;
		}

		bool found = false;
		int attempts = 0;
		int dir = rand() % 4;

		// starting in a random direction, check cardinal neighbors
		while ((!found) && attempts < 4)
		{
			if (! Executive::Instance().neighbor (seed, neighbor, (Direction) dir))
			{
				dir++;
				attempts++;
				continue;
			}

			// if this point hasn't been textured yet then we are ok to apply a texture to it now
			if (! points.in_set (neighbor.x, neighbor.y))
			{
				found = true;
			}
			attempts++;
		}

		// if (x,y) has no legal neighbors, then it must no longer be on the boundary
		if (! found)
		{
			boundary.remove (x, y);
			continue;
		}

		// apply texture to this point
		texture -> SetPrimary (neighbor.x, neighbor.y, indexOf (tex), 255);
		texture -> SetSecondary (neighbor.x, neighbor.y, indexOf (tex), 255);

		// add the neighbor point to the blob, and the boundary set
		points.insert (neighbor.x, neighbor.y);
		boundary.insert (neighbor.x, neighbor.y);		// assume this is on the boundary
		placed++;
	}

	return true;
}

// -------------------------------------------------------------
// -------------------------------------------------------------
bool Culture_Generator::find_flat_region (Point& p)
{
	bool found = false;
	int attempts = 0;

	while ((! found) && (attempts < 3000))
	{
		Point point;
		Executive::Instance().random_land (point);
		if (is_flat_region (point))
		{
			p.x = point.x;
			p.y = point.y;
			return true;
		}

		attempts++;
	}

	return false;
}

// -------------------------------------------------------------
// is_flat_region -- determine if a point lies on a relatively flat region
// -------------------------------------------------------------
bool Culture_Generator::is_flat_region (Point& p)
{
	float tolerance = 0.08f;
	int x = p.x;
	int y = p.y;
	float h1 = (float) Executive::Instance().getHeight(p);


	// relatively flat means within tolerance of height at samples
	for (int i = -1; i < 2; i++)
	{
		for (int j = -1; j < 2; j++)
		{
			if ((i == x) && (j == y))
				continue;

			Point p2(x + (5*i), y + (5*j));

			float h2 = (float) Executive::Instance().getHeight(p2);

			float delta = h1 - h2;
			float ratio = fabs(delta / h1);

			if (ratio > tolerance)
			{
				return false;
			}
		}
	}

	return true;
}

// ==========================================================
// write_texture_page -- write out one page of texture index data
// ==========================================================
void Culture_Generator::write_texture_page (int page_x, int page_y)
{
	Params& params = Params::Instance();

	int x1 = page_x * params.page_size;
	int x2 = x1 + params.page_size;
	int y1 = page_y * params.page_size;
	int y2 = y1 + params.page_size;

	std::ostringstream filename;
	filename << "./split/mapgen3.Index." << page_y << "." <<
		page_x << ".png";
	texture -> Write (filename.str().c_str(), x1, y1, x2, y2);

	filename.str() = "";
}

// ==========================================================
// SplitMap -- split the heightmap and textures up into pages
// ==========================================================
void Culture_Generator::SplitMap (void)
{	
	Params& params = Params::Instance();

	for (int page_x = 0; page_x < params.num_x_pages; page_x++)
	{
		for (int page_y = 0; page_y < params.num_y_pages; page_y++)
		{
			write_texture_page (page_x, page_y);
        }
    }
}

