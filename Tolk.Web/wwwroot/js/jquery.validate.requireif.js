function isNullOrEmpty(val) {
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

$.validator.unobtrusive.adapters.add('requiredif', ['otherproperty', 'otherpropertytype', 'otherpropertyvalue'], function (options) {
    var element = $(options.form).find("#" + options.params['otherproperty'])[0];
    options.rules['requiredif'] = [element, options.params['otherpropertytype'], options.params['otherpropertyvalue']];
    options.messages['requiredif'] = options.message;
});
