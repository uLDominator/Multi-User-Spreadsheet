all: compile

run: compile
	touch spreadsheet_files.txt
	clear
	./spreadsheet_server.cool
	
compile:
	g++ -o spreadsheet_server.cool server.cc -lboost_system -lpthread -lboost_thread-mt
	
clean:
	rm -f *.xml *.o spreadsheet_files.txt *~ 
	touch spreadsheet_files.txt