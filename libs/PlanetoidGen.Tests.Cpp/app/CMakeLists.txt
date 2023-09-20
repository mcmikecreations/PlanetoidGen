project(app)

file(GLOB APP_SRC ./src/**/*.cc)
file(GLOB APP_INC ./include/**/*.h)

file(GLOB APP_SRC_BASE ./src/*.cc)
file(GLOB APP_INC_BASE ./include/*.h)

list(APPEND APP_SRC ${APP_SRC_BASE})
list(APPEND APP_INC ${APP_INC_BASE})

set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${CMAKE_SOURCE_DIR}/cmake/")
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
# add_compile_options(-Wall -Wextra -Wvla)
# set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fsanitize=address")

if (APPLE)
	list(APPEND CMAKE_PREFIX_PATH /usr/local/opt/bison)
	list(APPEND CMAKE_PREFIX_PATH /usr/local/opt/flex)
endif (APPLE)

if (CYGWIN)
	set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} -static-libstdc++")
endif (CYGWIN)

add_executable(${PROJECT_NAME}
	${APP_SRC}
	${APP_INC}
    "src/stb/stb_image.cc" "src/stb/stb_image_write.cc"
)

target_link_libraries(${PROJECT_NAME})

target_include_directories(${PROJECT_NAME}
	PUBLIC
		$<INSTALL_INTERFACE:include>
		$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
		$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include/Agents>
		$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include/TerrainOps>
		$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include/stb>
	PRIVATE
		${CMAKE_CURRENT_SOURCE_DIR}/src
)

target_compile_features(${PROJECT_NAME}
	PRIVATE
		cxx_std_11
)

add_custom_command(
	TARGET ${PROJECT_NAME} POST_BUILD
	COMMAND ${CMAKE_COMMAND} 
        -DSOURCE_PATH="${CMAKE_CURRENT_SOURCE_DIR}/../data/"
        -DTARGET_PATH="${CMAKE_BINARY_DIR}/app/"
        -P ${CMAKE_SOURCE_DIR}/cmake/data_copy.cmake
    COMMENT "Running data copy script..."
    WORKING_DIRECTORY ${CMAKE_SOURCE_DIR})
