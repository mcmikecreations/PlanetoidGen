#include "index.h"

Index::Index (uint x, uint y) : Image (x, y)
{
	for (int i = 0; i < x; i++)
		for (int j = 0; j < y; j++)
			Set(i,j, 0);
}

Index::~Index ()
{
}

// ===================================================================
// Setter functions to store primary and secondary texture data
//
// Inputs:
//		x, y:  The map location to update
//		texture_id:		The slice number of the texture
//		alpha:			The alpha value associated with this point
// ===================================================================

void Index::SetPrimary (uint x, uint y, uchar texture_id, uchar alpha)
{
	if (! in_range (x, y))
	{
		fprintf (stderr, "SetPrimary: (%d,%d) out of range\n", x, y);
		return;
	}

	ulong current = Get (x, y);
	ulong next = (texture_id << 24) | (alpha << 16) |
		 (current & 0xffff);
	Set (x, y, next);
}

void Index::SetSecondary (uint x, uint y, uchar texture_id, uchar alpha)
{
	if (! in_range (x, y))
	{
		fprintf (stderr, "SetSecondary: (%d,%d) out of range\n", x, y);
		return;
	}

	ulong current = Get (x, y);
	ulong next = (current & 0xffff0000) |
		(texture_id << 8) | alpha;
	Set (x, y, next);
}

// ===================================================================
// A series of access functions for the primary and seconary texture data
//
// Inputs:
//		x, y:	The map location to retrieve
// ===================================================================

uchar Index::PrimaryTexture (uint x, uint y)
{
	if (! in_range (x, y))
		return 0;

	ulong value = Get (x, y);
	
	return (value >> 24) & 0xff;
}
uchar Index::PrimaryAlpha (uint x, uint y)
{
	if (! in_range (x, y))
		return 0;
	ulong value = Get (x, y);
	
	return (value >> 16) & 0xff;
}
uchar Index::SecondaryTexture (uint x, uint y)
{
	if (! in_range (x, y))
		return 0;
	ulong value = Get (x, y);

	return (value >> 8) & 0xff;
}
uchar Index::SecondaryAlpha (uint x, uint y)
{
	if (! in_range (x, y))
		return 0;
	ulong value = Get (x, y);

	return value & 0xff;
}


