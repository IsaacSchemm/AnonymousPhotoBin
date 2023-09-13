// https://github.com/rapito/knockout-orderable/tree/multi-sort

ko.bindingHandlers.orderable = {
    getProperty: function(o, s) {
        // copied from http://stackoverflow.com/questions/6491463/accessing-nested-javascript-objects-with-string-key
        s = s.replace(/\[(\w+)\]/g, '.$1'); // convert indexes to properties
        s = s.replace(/^\./, '');           // strip a leading dot
        var a = s.split('.');
        while (a.length) {
            var n = a.shift();
            if (n in o) {
                o = o[n];
            } else {
                return;
            }
        }
        return o;
    },

    // Extracts value from a field if its a function or not.
    getVal: function(object, string){
        // first getProperty/field out of object
        var field = ko.bindingHandlers.orderable.getProperty(object, string);
        // then get the val if its a function or not.
        return (typeof field === 'function') ?  field() :  field; 
    },

    compare: function (left, right) {
		
        left = left == null ? '' : left;
        right = right == null ? '' : right;
		
        //if (typeof left === 'string' || typeof right === 'string') {
        //    return left ? left.localeCompare(right) : 1;
        //}
		
        if (left > right)
            return 1;

        return left < right ? -1 : 0;
    },

    //get all sort results of thenBy fields
    sortThenBy: function(left, right, field, thenBy, orderDirection){
        var sortResults = []; 

        if(!thenBy)  return sortResults;

        var thenByFields = thenBy.split(','); // extract fields
        //console.log('sortResults', thenByFields, left, right, field);

        for (var i = 0; i < thenByFields.length; i++) {

            var tbField = thenByFields[i].trim();
            var lv = ko.bindingHandlers.orderable.getVal(left, tbField);
            var rv = ko.bindingHandlers.orderable.getVal(right, tbField);
            var sort = 0;

            if(orderDirection == "desc") {
                sort = ko.bindingHandlers.orderable.compare(rv, lv);
            } else {
                sort = ko.bindingHandlers.orderable.compare(lv, rv);
            }

            //console.log('sortResults', lv, rv, sort);
            sortResults.push(sort);
        }   

        return sortResults;
    },

    sort: function (viewModel, collection, field, thenBy) {
        var orderDirection = viewModel[collection].orderDirection();

        //make sure we sort only once and not for every binding set on table header
        if (viewModel[collection].orderField() == field) {
            viewModel[collection].sort(function (left, right) {
                var leftVal  = ko.bindingHandlers.orderable.getVal(left, field);
                var rightVal = ko.bindingHandlers.orderable.getVal(right, field);

                // these will hold all fields for the thenBy fields
                // evaluate all thenBy compare first
                var thenByResults = ko.bindingHandlers.orderable.sortThenBy(left, right, field, thenBy, orderDirection);

                var sort = 0;

                if (orderDirection == "desc") {
                    sort = ko.bindingHandlers.orderable.compare(rightVal, leftVal);
                } else {
                    sort = ko.bindingHandlers.orderable.compare(leftVal, rightVal);
                }

                // sort then by fields in same order
                if(thenByResults.length > 0){ 
                    for (var i = 0; i < thenByResults.length; i++) {
                        sort = sort || thenByResults[i];
                    }
                }

                return sort;
            });
        }
    },

    init: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
        //get provided options
        var collection = valueAccessor().collection;
        var field = valueAccessor().field;
        var thenBy = valueAccessor().thenBy;

        //add a few observables to ViewModel to track order field, direction, and then by fields
        if (viewModel[collection].orderField == undefined) {
            viewModel[collection].orderField = ko.observable();
        }
        if (viewModel[collection].orderDirection == undefined) {
            viewModel[collection].orderDirection = ko.observable("asc");
        }
        if (viewModel[collection].orderThenByFields == undefined) {
            viewModel[collection].orderThenByFields = ko.observable();
        }

        var defaultField = valueAccessor().defaultField;
        var defaultDirection = valueAccessor().defaultDirection || "asc";
        var defaultThenBy = valueAccessor().defaultThenBy || null;
        if (defaultField) {
            viewModel[collection].orderField(field);            
            viewModel[collection].orderDirection(defaultDirection);
            viewModel[collection].orderThenByFields(defaultThenBy);
            ko.bindingHandlers.orderable.sort(viewModel, collection, field, thenBy);
        }

        //set order observables on table header click
        $(element).click(function (e) {
            e.preventDefault();
            
            //flip sort direction if current sort field is clicked again
            if (viewModel[collection].orderField() == field) {
                if (viewModel[collection].orderDirection() == "asc") {
                    viewModel[collection].orderDirection("desc");
                } else {
                    viewModel[collection].orderDirection("asc");
                }
            }
            
            viewModel[collection].orderField(field);
            viewModel[collection].orderThenByFields(thenBy);
        });

        //order records when observables changes, so ordering can be changed programmatically
        viewModel[collection].orderField.subscribe(function () {
            ko.bindingHandlers.orderable.sort(viewModel, collection, field, thenBy);
        });
        viewModel[collection].orderDirection.subscribe(function () {
            ko.bindingHandlers.orderable.sort(viewModel, collection, field, thenBy);
        });
    },

    update: function (element, valueAccessor, allBindingsAccessor, viewModel, bindingContext) {
        //get provided options
        var collection = valueAccessor().collection;
        var field = valueAccessor().field;
        var isOrderedByThisField = viewModel[collection].orderField() == field;
            
        //apply css binding programmatically
        ko.bindingHandlers.css.update(
            element,
            function () {
                return {
                    sorted: isOrderedByThisField,
                    asc: isOrderedByThisField && viewModel[collection].orderDirection() == "asc",
                    desc: isOrderedByThisField && viewModel[collection].orderDirection() == "desc"
                };
            },
            allBindingsAccessor,
            viewModel,
            bindingContext
        );
    }
};
