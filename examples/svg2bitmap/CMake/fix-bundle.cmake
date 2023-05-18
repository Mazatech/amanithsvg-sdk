
# because this script will be executed by CMake in script mode (i.e. cmake -P)
# we must ensure that some basic system variables are set correctly (e.g. CMAKE_HOST_SYSTEM_NAME)
include(CMake/checkSystem.cmake)
# if a toolchain file is specified, include it
if (DEFINED CMAKE_TOOLCHAIN_FILE)
    include(${CMAKE_TOOLCHAIN_FILE})
endif()

# compiling for a Darwin-based system (MacOS X, iOS)
if (${CMAKE_SYSTEM_NAME} MATCHES "Darwin")

    message(STATUS "Making ${TARGET_EXECUTABLE_FNAME} a standlaone self-contained application")

    message(STATUS "\tCopying AmanithSVG library within ${TARGET_EXECUTABLE_DIR}")
    # copy AmanithSVG library within the executable location
    file(GLOB AMANITHSVG_LIB_FNAME_ABS "${AMANITHSVG_LIBS_DIR}/libAmanithSVG.?.dylib")
    get_filename_component(AMANITHSVG_LIB_FNAME ${AMANITHSVG_LIB_FNAME_ABS} NAME)
    execute_process(COMMAND cp ${AMANITHSVG_LIB_FNAME_ABS} ${TARGET_EXECUTABLE_DIR} RESULT_VARIABLE result)

    if (NOT result EQUAL 0)
        message(FATAL_ERROR "AmanithSVG library copy FAILED!")
    else()
        message(STATUS "\tRelinking executable and AmanithSVG library, in order to create a standalone application")
        # now relink the executable and AmanithSVG library, in order to create a standalone application
        if (DEFINED CMAKE_INSTALL_NAME_TOOL)
            # change the install name of the AmanithSVG library
            execute_process(COMMAND ${CMAKE_INSTALL_NAME_TOOL} -id "@executable_path/${AMANITHSVG_LIB_FNAME}" ${TARGET_EXECUTABLE_DIR}/${AMANITHSVG_LIB_FNAME} RESULT_VARIABLE result)
            if (NOT result EQUAL 0)
                message(FATAL_ERROR "Changing AmanithSVG library install name FAILED!")
            else()
                # change the executable dependency
                execute_process(COMMAND ${CMAKE_INSTALL_NAME_TOOL} -change "@rpath/${AMANITHSVG_LIB_FNAME}" "@executable_path/${AMANITHSVG_LIB_FNAME}" ${TARGET_EXECUTABLE_DIR}/${TARGET_EXECUTABLE_FNAME} RESULT_VARIABLE result)
                if (NOT result EQUAL 0)
                    message(FATAL_ERROR "Changing executable dependency FAILED!")
                endif()
            endif()
        else()
            message(FATAL_ERROR "Unable to find 'install_name_tool' program (CMAKE_INSTALL_NAME_TOOL variable has not been set within the ${CMAKE_TOOLCHAIN_FILE} toolchain file)!")
        endif()
    endif()

endif()
