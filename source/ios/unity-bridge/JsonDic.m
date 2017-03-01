//
// Created by Aleksey Zinchenko on 01/03/2017.
// Copyright (c) 2017 voximplant. All rights reserved.
//

#import "JsonDic.h"

@interface JsonDic ()

@end

@implementation JsonDic

- (instancetype)initWithJSONString:(NSString *)JSONString {
    self = [super init];
    if (self) {

        NSError *error = nil;
        NSData *JSONData = [JSONString dataUsingEncoding:NSUTF8StringEncoding];
        NSDictionary *JSONDictionary = [NSJSONSerialization JSONObjectWithData:JSONData options:0 error:&error];

        if (!error && JSONDictionary) {

            //Loop method
            for (NSString *key in JSONDictionary) {
                [self setValue:[JSONDictionary valueForKey:key] forKey:key];
            }

            if (_list != Nil) {
                _dic = [[NSMutableDictionary alloc] init];
                for (NSDictionary *item in _list) {
                    [_dic setValue:item[@"value"] forKey:item[@"key"]];
                }
            }
        }
    }
    return self;
}

@end