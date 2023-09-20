# PlanetoidGen

Modern journalistic and educational environments heavily rely on interactive visualization
of landscapes to cover historical events and explain them to a broader audience.
This is especially valuable for disaster coverage, such as destruction consequences,
where a wider outlook on a large area is required. While there is some research covering
the imaging of large-scale data, the content generation aspect is still performed either
manually or by utilizing outdated techniques.

This research builds on the advancements in multiple academic domains, including
databases, distributed systems, cloud computing, and parallel execution, to allow data
processing and content generation algorithms to use state-of-the-art hardware and
software to a better extent, focusing on performance, flexibility, and extensibility of the
proposed system.

In this work, a common software agent interface is provided for use by external
researchers, and several implementations are provided. The main one covers the goal of
this research to produce 3D models for "before and after" comparisons of cities in eastern
Ukraine. An auxiliary set of agents exists generating procedural terrain for performance analysis.

## Technology stack

The solution is provided as a set of microservices running on Kubernetes. You can find the
initialization scripts in the `./build/` folder. This includes:
- MongoDB for 3D model storage.
- PostgreSQL for vector data and metadata storage.
- ASP.NET Core for the processing server and software agents.
- Kafka for the message queue broker.
- Unity for the client of choice.

Communication between the client and the server happens over gRPC and REST. The software agent
implementations are loaded as dynamic libraries based on file paths in the configuration.

## Build

To build the solution, pre-install the technologies mentioned above. The C# dependencies are
auto-installed during build time, including ProtoBuf models and services. The metadata and
vertor databases are also filled with tables etc. on first launch. Configure the microservice
addresses in Kubernetes and ASP.NET Core configuration files.

## Aknowledgements

I want to thank Prof. Dr. Asanobu Kitamoto from the National Institute of Informatics
for guiding me during the research and helping me with the storytelling
aspect of the project, Prof. Dr. Hidenori Watanave from the University of Tokyo
for giving advice within the domain of historical events coverage as well as Tarin
Clanuwat and Yingtao Tian from Google Japan on the technical details of the solution.

I also want to thank Prof. Dr. Yevheniya Levus, M. Sc. Roman Moravskyi, and
Pavlo Pustelnyk from Lviv Polytechnic National University for using the developed
system in their research and providing baseline from their systems for the benchmark.

Some of the developed storytelling software agents use the Overpass Application
Programming Interface (API) and can be optionally configured to use a local instance in a
Docker container. The Overpass project follows the GNU Affero General Public License
v3.0. Map data copyrighted OpenStreetMap contributors and available from https:
//www.openstreetmap.org. Another storytelling software agent uses the Mapbox API
for satellite images.

## Papers

Y. Levus, P. Pustelnyk, R. Moravskyi and M. Morozov, “Architecture of a Distributed Software System for Procedural Planetoid Terrain Generation,” Ukrainian Journal of Information Technology, vol. 5, no. 1, 2023, doi: https://doi.org/10.23939/ujit2023.01

Y. Levus, R. Westermann, M. Morozov, R. Moravskyi and P. Pustelnyk, “Using Software Agents in a Distributed Computing System for Procedural Planetoid Terrain Generation,” 2022 IEEE 17th International Conference on Computer Sciences and Information Technologies (CSIT), Lviv, Ukraine, 2022, pp. 446-449, doi: https://doi.org/10.1109/CSIT56902.2022.10000868.

M. Y. Morozov, R. O. Moravskyi, P. Y. Pustelnyk, and Y. V. Levus, “Algorithms and Architecture of the Software System of Automated Natural and Anthropogenic Landscape Generation,” Radio Electronics, Computer Science, Control, vol. 61, no. 2, pp. 154–164, 2022, doi: https://doi.org/10.15588/1607-3274-2022-2-15.

M. Y. Morozov, R. O. Moravskyi, P. Y. Pustelnyk, and Y. V. Levus, “Containerization method for visualization of natural and anthropogenic landscapes,” Scientific Bulletin of UNFU, vol. 31, no. 5, pp. 90–95, 2021, doi: https://doi.org/10.36930/40310514.

M. Y. Morozov, R. O. Moravskyi, P. Y. Pustelnyk, and Y. V. Levus, “Landscape Generation for Spherical Surfaces: Problem Analysis and Solution,” Scientific Bulletin of UNFU, vol. 30, no. 1, pp. 136–141, 2020, doi: https://doi.org/10.36930/40300124.
