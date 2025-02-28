# Script that copies files from a root folder (assumed to be iPhone's folder layout)
# to consolidated destination folders while preserving file metadata.
# Currently tested against an iPhone 14's folder structure, which is like:
#   202110__/
#     IMG_3068.HEIC
#     IMG_3083.HEIC
#   202201__/
#     GMVW4192.HEIC
#
# python import_from_iphone.py -i D:/Users/gglas/Devel/Utils/TestiPhoneImportScript/input -o D:/Users/gglas/Devel/Utils/TestiPhoneImportScript/output

import argparse
from datetime import datetime
import os
from pathlib import Path
import re
import shutil
import sys

class Params:
  inputpath: str = ''
  outputpath: str = ''

dry_run = False
max_files_per_output_folder = 500
total_files_processed = 0

# Get path containing this script,
# regardless of current working directory
def get_script_path() -> str:
  return os.path.dirname(os.path.realpath(__file__))

def delete_files_and_folders_under_path(path: str):
  global dry_run

  with os.scandir(path) as entries:
    for entry in entries:
      if dry_run:
        print(f'would delete: {entry.path}')
      else:
        if entry.is_dir() and not entry.is_symlink():
          shutil.rmtree(entry.path)
        else:
          print(f'deleting {entry.path}')
          os.remove(entry.path)

def copy_file(src: str, dest: str):
  global dry_run

  if not dry_run:
    # copy2 attempts to preserve metadata
    shutil.copy2(src, dest)

def error_quit(error: str) -> int:
  if error != '':
    print(error)
  print('\nScript Failed.\n')
  return 1

def validate_params(p: Params) -> str:
  error = ''

  p.inputpath = os.path.abspath(p.inputpath)
  if not os.path.isdir(p.inputpath):
    return f'Error: inputpath "{p.inputpath}" not found'

  p.outputpath = os.path.abspath(p.outputpath)
  if not os.path.isdir(p.outputpath):
    return f'Error: outputpath "{p.outputpath}" not found'

  return error

# Finds and returns the next RawXXX_YYYYMMDD folder
def find_and_create_next_output_folder(outputpath: str) -> str:
  global dry_run

  # Regular expression to match the file format "RawXXX_YYYYMMDD"
  pattern = re.compile(r"^Raw(\d{3})_(\d{8})$")

  # The highest XXX found
  max_number = 0

  for item in os.listdir(outputpath):
    match = pattern.match(item)
    if match:
      number = int(match.group(1))
      max_number = max(max_number, number)

  # Increment max_number by 1 and format it as three digits
  next_number = f"{(max_number + 1):03}"

  # Form the next folder name
  current_date = datetime.now().strftime('%Y%m%d')
  next_base_folder = f"Raw{next_number}_{current_date}"
  next_folder = os.path.join(outputpath, next_base_folder)

  # Create the new folder
  if not dry_run:
    Path(next_folder).mkdir(parents=False, exist_ok=False)

  return next_folder

def main() -> int:
  global dry_run
  global max_files_per_output_folder
  global total_files_processed

  parser = argparse.ArgumentParser(
    description='Copies photos/videos from iPhone folders into destination folders.')
  parser.add_argument('-i', '--inputpath',
                      help='Input path',
                      required=True)
  parser.add_argument('-o', '--outputpath',
                      help='Output path',
                      required=True)
  parser.add_argument('-n', '--dryrun',
                      help='Dry run: no filesystem changes',
                      action='store_true')

  args = parser.parse_args()

  p = Params()
  p.inputpath = args.inputpath
  p.outputpath = args.outputpath
  dry_run = args.dryrun
  print(f'dry_run: {dry_run}')

  error = validate_params(p)
  if error != '':
    return error_quit(error)

  print(f'Importing iPhone photos/videos from [{p.inputpath}] to [{p.outputpath}]...')

  #script_path = get_script_path()

  # Finds and creates the next RawXXX_YYYYMMDD folder to copy files to
  current_output_folder = find_and_create_next_output_folder(p.outputpath)
  print(f'Current output folder: [{current_output_folder}]')
  num_files_per_output_folder = 0

  with os.scandir(p.inputpath) as entries:
    for entry in entries:
      #print(f'entry: {entry.path}')

      if entry.is_dir() and not entry.is_symlink():
        # process entries within the folder
        print(f'Processing input folder: {entry.path}')
        parent_folder_name = os.path.basename(entry.path)

        with os.scandir(entry.path) as inner_entries:
          for inner_entry in inner_entries:
            #print(f'inner_entry: {inner_entry.path}')

            if inner_entry.is_dir():
              print(f'Skipping unexpected child folder: {inner_entry.path}')
              continue

            if inner_entry.is_symlink():
              print(f'Skipping unexpected symlink: {inner_entry.path}')
              continue

            if inner_entry.is_file():
              print(f'Found file: {inner_entry.path}')
              
              # Files can have the same name across iPhones subfolders.
              # Since we are trying to consolidate files into less output folders,
              # we need a way to rename the files so there are no name clashses,
              # which is what this does.
              base_input_file_name = os.path.basename(inner_entry.path)
              base_output_file_name = f'{parent_folder_name}_{base_input_file_name}'
              output_file_path = os.path.join(current_output_folder, base_output_file_name)

              print(f'Output file: [{output_file_path}]')

              copy_file(inner_entry.path, output_file_path)

              num_files_per_output_folder += 1
              total_files_processed += 1

            if num_files_per_output_folder >= max_files_per_output_folder:
                # Finds and creates the next RawXXX_YYYYMMDD folder to copy files to
                current_output_folder = find_and_create_next_output_folder(p.outputpath)
                print(f'Current output folder: [{current_output_folder}]')
                num_files_per_output_folder = 0
      else:
        print(f'Skipping unexpected file not in a folder: {entry.path}')
      # END if entry.is_dir() and not entry.is_symlink():
    # END for entry in entries:
  # END with os.scandir(p.inputpath) as entries:

  # TODO: When 100% sure things are working correctly, delete source folder's children.
  #       Only if -D (capital D only) is present, and not dry_run

  print(f'total_files_processed: {total_files_processed}')

  return 0

if __name__ == '__main__':
  sys.exit(main())
