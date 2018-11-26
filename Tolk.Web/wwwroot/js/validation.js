﻿function isNullOrEmpty(val) {
    return val == null /* eslint-disable-line eqeqeq */
        || val == undefined /* eslint-disable-line eqeqeq */
        || val == ''; /* eslint-disable-line eqeqeq */
}

$.validator.addMethod('requiredif', function (value, element, params) {
    var otherElement = params[0];
    switch (params[1]) {
        case "notnull":
            if (!isNullOrEmpty(params[2])) {
                return !isNullOrEmpty(value);
            }
            return true;
        case "System.Boolean":
            var booleanValue = params[2] == 'True' ? true : false; /* eslint-disable-line eqeqeq */
            if (otherElement.checked === booleanValue) {
                return !isNullOrEmpty(value);
            }
            return true;
        case "System.Integer":
        case "System.Long":
        case "System.Float":
        case "System.Double":
            if (Number($(otherElement).val()) === Number(params[2])) {
                return !isNullOrEmpty(value);
            }
            return true;
        case "System.String":
        default:
            if ($(otherElement).val() === params[2]) {
                return !isNullOrEmpty(value);
            }
            return true;
    }
});

$.validator.addMethod('requiredchecked', function (value, element, params) {
    if (!$(element).hasClass("force-validation")) {
        return true;
    }

    var cbGroup = $(element).parent();
    if (cbGroup) {
        var min = Number(params[0]);
        var max = Number(params[1]);
        var maxChecked = Number(params[2]);
        var message = params[3];
        var checkboxGroupId = cbGroup.attr("for");
        var validationTag = $('span[data-valmsg-for="' + checkboxGroupId + '"]');

        if (min === 0 && max >= maxChecked) {
            validationTag.html('');
            return true;
        }

        var allCheckboxes = $('[data-checkbox-group="' + checkboxGroupId + '"]');
        var allCheckedCheckedboxes = allCheckboxes.filter(':checked');

        if (allCheckedCheckedboxes.length < min || allCheckedCheckedboxes.length > max) {
            validationTag.html(message);
            return false;
        }

        validationTag.html('');
    }

    return true;
});

$.validator.unobtrusive.adapters.add('requiredif', ['otherproperty', 'otherpropertytype', 'otherpropertyvalue'], function (options) {
    var element = $(options.form).find("#" + options.params['otherproperty'])[0];
    options.rules['requiredif'] = [element, options.params['otherpropertytype'], options.params['otherpropertyvalue']];
    options.messages['requiredif'] = options.message;
});

$.validator.unobtrusive.adapters.add('requiredchecked', ['min', 'max', 'maxchecked'], function (options) {
    options.rules['requiredchecked'] = [options.params['min'], options.params['max'], options.params['maxchecked'], options.message];
    options.messages['requiredchecked'] = options.message;
});