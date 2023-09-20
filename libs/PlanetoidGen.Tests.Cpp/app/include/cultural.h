#ifndef CULTURAL_H
#define CULTURAL_H

#include "heightmap.h"

class Index;
class PointSet;

struct cPoint
{
	float x;
	float y;
	float z;
};

class Culture_Generator
{
	private:
		Heightmap *height;
		Index *texture;

		bool texture_blob (PointSet& points, int x, int y, int tex, int size);

		bool find_flat_region (Point& p);
		bool is_flat_region (Point& p);

		void generate_textures (void);
		int max_gradient (int x, int y);

		int indexOf (int slice);
		bool TexturedAs (int x, int y, int texture_id);
		bool AnySurrounding (int x, int y, int texture_id);

		void write_texture_page (int page_x, int page_y);

		// translate to dest map coords
		void translate (int x, int y, cPoint& translated);
		void place_mesh (std::ofstream &, std::string name, float x, float y,
			float yaw, float scale);
	public:
		Culture_Generator ();
		virtual ~Culture_Generator ();

		void generate (void);
		void SplitMap (void);
};

#endif
