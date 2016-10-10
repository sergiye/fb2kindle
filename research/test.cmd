cd test
"..\tar" -cv --file="..\test.tar" *
"..\gzip" -9 < "..\test.tar" > "..\test.mobi"
del "..\test.tar"